using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.TestSupport;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Models;

namespace ComicChess.KnowledgeDemon;

/// <summary>
/// 独一：牌组入牌时静默变化；战斗内各牌堆重复则在 <see cref="AfterCardChangedPiles"/> 后变化（带特效）。
/// </summary>
[RegisterSingleton]
public sealed class KnowledgeDemonUniqueSingleton : HookedSingletonModel
{
    public static KnowledgeDemonUniqueSingleton? Instance { get; private set; }

    private readonly HashSet<Player> _scheduledCombatResolutionPlayers = [];
    private bool _resolvingCombatViolations;
    private bool _combatResolutionDrainScheduled;

    public KnowledgeDemonUniqueSingleton()
        : base(HookType.Combat)
    {
        Instance = this;
        ModHelper.SubscribeForRunStateHooks(Id.Entry, _ => [this]);
    }

    public override bool TryModifyCardBeingAddedToDeck(CardModel card, out CardModel? newCard)
    {
        newCard = null;
        if (card.Owner is not { } player)
        {
            return false;
        }

        var resolved = KnowledgeDemonUniqueUtility.ResolveDeckAddition(player, card);
        if (resolved == card)
        {
            return false;
        }

        PlayDeckTransformPreviewDeferred(card, resolved);
        newCard = resolved;
        return true;
    }

    public override Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        AbstractModel? clonedBy)
    {
        _ = oldPileType;
        _ = clonedBy;

        if (card.Owner is not { } player || card.CombatState == null)
        {
            return Task.CompletedTask;
        }

        if (card.Pile is not { IsCombatPile: true })
        {
            return Task.CompletedTask;
        }

        return ResolveCombatViolationsAsync(player, card);
    }

    public async Task AddUniqueKeywordsAndResolveAsync(Player player, IEnumerable<CardModel> cards)
    {
        CardModel? preferredDuplicate = null;

        foreach (var card in cards)
        {
            if (!AddUniqueKeyword(card))
            {
                continue;
            }

            preferredDuplicate ??= card;
        }

        if (preferredDuplicate is not null)
        {
            await ResolveCombatViolationsAsync(player, preferredDuplicate);
        }
    }

    public void AddUniqueKeywordsAndQueueResolution(Player player, IEnumerable<CardModel> cards)
    {
        foreach (var card in cards)
        {
            AddUniqueKeyword(card);
        }

        QueueCombatViolationResolution(player);
    }

    public void QueueCombatViolationResolution(Player player)
    {
        _scheduledCombatResolutionPlayers.Add(player);
        if (_combatResolutionDrainScheduled)
        {
            return;
        }

        _combatResolutionDrainScheduled = true;
        Callable.From(DrainScheduledCombatResolutions).CallDeferred();
    }

    private void DrainScheduledCombatResolutions()
    {
        _ = TaskHelper.RunSafely(DrainScheduledCombatResolutionsAsync());
    }

    private async Task DrainScheduledCombatResolutionsAsync()
    {
        _combatResolutionDrainScheduled = false;
        var players = _scheduledCombatResolutionPlayers.ToArray();
        _scheduledCombatResolutionPlayers.Clear();

        foreach (var player in players)
        {
            await ResolveCombatViolationsAsync(player);
        }
    }

    private async Task ResolveCombatViolationsAsync(Player player, CardModel? preferredDuplicate = null)
    {
        if (_resolvingCombatViolations)
        {
            return;
        }

        _resolvingCombatViolations = true;
        try
        {
            var iterationsLeft = System.Math.Max(32, KnowledgeDemonUniqueUtility.IterateCombatUniqueScope(player).Count() * 8);
            while (TryFindDuplicateUnique(player, preferredDuplicate) is { } duplicate)
            {
                if (--iterationsLeft < 0)
                {
                    Entry.Logger.Warn("[Unique] Aborted duplicate-resolution loop after too many transforms.");
                    return;
                }

                preferredDuplicate = null;
                if (!duplicate.IsTransformable)
                {
                    return;
                }

                var replacement = TryCreateCombatUniqueReplacement(player, duplicate);
                if (replacement is null)
                {
                    return;
                }

                replacement = KnowledgeDemonUniqueUtility.CreateStatePreservingReplacement(duplicate, replacement);
                await BookLibraryUtility.TransformCard(duplicate, replacement);
            }
        }
        finally
        {
            _resolvingCombatViolations = false;
        }
    }

    private static CardModel? TryCreateCombatUniqueReplacement(Player player, CardModel duplicate)
    {
        var cardScope = duplicate.CardScope;
        if (cardScope is null)
        {
            return null;
        }

        if (duplicate.Type == CardType.Status
            && player.Creature.GetPower<EnvironmentalTolerancePower>() is not null)
        {
            var candidates = TransformOptionUtility
                .GetEnvironmentalToleranceTransformCandidates(player, duplicate, duplicate.IsInCombat)
                .Where(candidate =>
                    !KnowledgeDemonUniqueUtility.WouldViolateCombatUniqueRule(player, candidate, duplicate))
                .GroupBy(card => card.Id)
                .Select(group => group.First())
                .ToArray();

            if (candidates.Length > 0)
            {
                var selected = player.RunState.Rng.Niche.NextItem(candidates);
                return selected is null ? null : cardScope.CreateCard(selected, player);
            }

            if (TransformOptionUtility.GetInfiniteCard() is { } infinite)
            {
                return cardScope.CreateCard(infinite, player);
            }

            return null;
        }

        var replacement = new CardTransformation(duplicate).GetReplacement(player.RunState.Rng.Niche);
        if (replacement is null)
        {
            return null;
        }

        if (!KnowledgeDemonUniqueUtility.WouldViolateCombatUniqueRule(player, replacement, duplicate))
        {
            return replacement;
        }

        if (TransformOptionUtility.GetInfiniteCard() is { } fallbackInfinite)
        {
            return cardScope.CreateCard(fallbackInfinite, player);
        }

        return replacement;
    }

    private static CardModel? TryFindDuplicateUnique(Player player, CardModel? preferredDuplicate = null)
    {
        if (preferredDuplicate is not null
            && KnowledgeDemonUniqueUtility.ShouldTransformAsDuplicate(player, preferredDuplicate))
        {
            return preferredDuplicate;
        }

        var seen = new Dictionary<ModelId, CardModel>();

        foreach (var card in KnowledgeDemonUniqueUtility.IterateCombatUniqueScope(player))
        {
            if (!KnowledgeDemonUniqueUtility.IsUnique(card))
            {
                continue;
            }

            if (seen.TryGetValue(card.Id, out var first))
            {
                return KnowledgeDemonUniqueUtility.PreferNewerDuplicate(first, card);
            }

            seen[card.Id] = card;
        }

        return null;
    }

    private static void PlayDeckTransformPreviewDeferred(CardModel original, CardModel replacement)
    {
        if (TestMode.IsOn)
        {
            return;
        }

        Callable.From(() =>
        {
            if (replacement.Pile?.Type != PileType.Deck || NRun.Instance?.GlobalUi is not { } globalUi)
            {
                return;
            }

            var vfx = NCardTransformVfx.Create(original, replacement, null);
            if (vfx is not null)
            {
                globalUi.CardPreviewContainer.AddChild(vfx);
            }
        }).CallDeferred();
    }

    public static bool AddUniqueKeyword(CardModel card)
    {
        if (KnowledgeDemonUniqueUtility.IsImmuneToUnique(card)
            || KnowledgeDemonUniqueUtility.IsUnique(card))
        {
            return false;
        }

        card.AddKeyword(ModKeywordRegistry.GetCardKeyword(KnowledgeDemonKeyword.Unique));
        return true;
    }
}
