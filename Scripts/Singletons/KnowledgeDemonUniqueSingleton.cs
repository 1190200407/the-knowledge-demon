using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models;

namespace ComicChess.KnowledgeDemon;

/// <summary>
/// 独一：牌组入牌时静默变化；战斗内各牌堆重复则在 <see cref="AfterCardChangedPiles"/> 后变化（带特效）。
/// </summary>
[RegisterSingleton]
public sealed class KnowledgeDemonUniqueSingleton : HookedSingletonModel
{
    private bool _resolvingCombatViolations;

    public KnowledgeDemonUniqueSingleton()
        : base(HookType.Combat)
    {
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

        newCard = resolved;
        return true;
    }

    public override async Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        AbstractModel? clonedBy)
    {
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
                await BookLibraryUtility.TransformToRandom(
                    duplicate,
                    player.RunState.Rng.Niche);
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
}
