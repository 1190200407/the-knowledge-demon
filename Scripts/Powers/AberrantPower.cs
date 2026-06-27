using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class AberrantPower : KnowledgeDemonPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        _ = participants;
        _ = combatState;

        if (side != CombatSide.Player || Owner.Player is not { } player)
        {
            return;
        }

        Flash();
        await PlayerCmd.GainEnergy(Amount, player);

        var statusCandidates = ModelDb.CardPool<KnowledgeDemonCardPool>()
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Where(card => card.Type == CardType.Status)
            .Where(card => card.CanBeGeneratedInCombat)
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

        var generated = player.RunState.CreateCard(canonicalCard, player);
        await CardPileCmd.AddGeneratedCardToCombat(generated, PileType.Hand, player);
    }
}
