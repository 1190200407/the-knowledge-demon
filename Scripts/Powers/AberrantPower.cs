using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class AberrantPower : KnowledgeDemonPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.ForEnergy(this),
    ];

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        _ = participants;
        if (side != CombatSide.Player || Owner.Player is not { } player)
        {
            return;
        }

        Flash();
        await PlayerCmd.GainEnergy(Amount, player);

        var statusCandidates = ModelDb.CardPool<StatusCardPool>()
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Concat(
                ModelDb.CardPool<KnowledgeDemonCardPool>()
                    .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
                    .Where(card => card.Type == CardType.Status))
            .Where(card => card.Type == CardType.Status)
            .Where(card => card is not Infinite)
            .Where(card => card.CanBeGeneratedInCombat)
            .DistinctBy(card => card.Id)
            .ToList();
        if (statusCandidates.Count == 0)
        {
            return;
        }

        var canonicalCard = player.RunState.Rng.CombatCardGeneration.NextItem(statusCandidates);
        if (canonicalCard is null)
        {
            return;
        }

        var generated = combatState.CreateCard(canonicalCard, player);
        await CardPileCmd.AddGeneratedCardToCombat(generated, PileType.Hand, player);
    }
}
