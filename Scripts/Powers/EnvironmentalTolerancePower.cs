using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class EnvironmentalTolerancePower : KnowledgeDemonPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [KnowledgeDemonKeywordHoverTips.FromUnique()];

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _ = applier;
        _ = cardSource;

        if (Owner.Player is not { } player
            || KnowledgeDemonUniqueSingleton.Instance is not { } uniqueSingleton)
        {
            return;
        }

        var currentStatusCards = KnowledgeDemonUniqueUtility
            .IterateCombatUniqueScope(player)
            .Where(card => card.Owner == player && card.Type == CardType.Status)
            .ToList();

        await uniqueSingleton.AddUniqueKeywordsAndResolveAsync(player, currentStatusCards);
    }

    public override Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        AbstractModel? clonedBy)
    {
        _ = oldPileType;
        _ = clonedBy;

        if (Owner.Player is not { } player
            || card.Owner != player
            || card.CombatState is null
            || card.Pile is not { IsCombatPile: true }
            || card.Type != CardType.Status
            || KnowledgeDemonUniqueSingleton.Instance is not { } uniqueSingleton)
        {
            return Task.CompletedTask;
        }

        uniqueSingleton.AddUniqueKeywordsAndQueueResolution(player, [card]);
        return Task.CompletedTask;
    }
}
