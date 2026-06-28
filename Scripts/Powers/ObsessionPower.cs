using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class ObsessionPower : KnowledgeDemonPowerModel, IKnowledgeDemonEventListener
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<string> RegisteredKeywordIds =>
        [KnowledgeDemonKeyword.Materialize];

    [SavedProperty]
    public int LastTriggeredTurnNumber { get; set; }

    public Task<CardModel> ModifyMaterializeCard(Player player, CardModel sourceCard, CardModel materializedCard)
    {
        if (Owner.Player != player || materializedCard.Owner != player || !CanTriggerForCurrentTurn(materializedCard))
        {
            return Task.FromResult(materializedCard);
        }

        LastTriggeredTurnNumber = player.PlayerCombatState?.TurnNumber ?? 0;

        var copy = materializedCard.CreateClone();
        copy.EnergyCost.SetThisCombat(0);
        copy.InvokeEnergyCostChanged();

        Flash();
        return Task.FromResult((CardModel)copy);
    }

    public override Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        _ = choiceContext;
        _ = participants;
        _ = combatState;

        if (side != CombatSide.Player || Owner.Player is null)
        {
            return Task.CompletedTask;
        }

        LastTriggeredTurnNumber = 0;
        return Task.CompletedTask;
    }

    private bool CanTriggerForCurrentTurn(CardModel materializedCard)
    {
        var combatState = materializedCard.CombatState;
        if (combatState is null || materializedCard.Owner?.PlayerCombatState is null)
        {
            return true;
        }

        var currentTurnNumber = materializedCard.Owner.PlayerCombatState.TurnNumber;
        if (LastTriggeredTurnNumber == currentTurnNumber)
        {
            return false;
        }

        return !CombatManager.Instance.History.Entries.OfType<CardGeneratedEntry>()
            .Any(entry => entry.Creator == materializedCard.Owner && entry.HappenedThisTurn(combatState));
    }
}
