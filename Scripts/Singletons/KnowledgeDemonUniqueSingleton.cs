using System.Collections.Generic;
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

    private bool _resolvingCombatViolations;

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

    public override async Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        AbstractModel? clonedBy)
    {
        _ = oldPileType;
        _ = clonedBy;

        if (card.Owner is not { } player || card.CombatState == null)
        {
            return;
        }

        if (card.Pile is not { IsCombatPile: true })
        {
            return;
        }

        await ResolveCombatViolationsAsync(player, card);
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

    private async Task ResolveCombatViolationsAsync(Player player, CardModel? preferredDuplicate = null)
    {
        if (_resolvingCombatViolations)
        {
            return;
        }

        _resolvingCombatViolations = true;
        try
        {
            while (TryFindDuplicateUnique(player, preferredDuplicate) is { } duplicate)
            {
                preferredDuplicate = null;
                if (!duplicate.IsTransformable)
                {
                    return;
                }

                var replacement = new CardTransformation(duplicate).GetReplacement(player.RunState.Rng.Niche);
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
