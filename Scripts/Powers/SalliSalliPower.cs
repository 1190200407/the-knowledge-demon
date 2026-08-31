using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class SalliSalliPower : KnowledgeDemonPowerModel
{
    private sealed class Data
    {
        public string? previousCardId;
        public int remainingTriggers;
        public int syncedTurnNumber = -1;
        public int syncedAmount;
        public bool initialized;
    }

    private const string PreviousCardVarName = "PreviousCard";

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => GetInternalData<Data>().remainingTriggers;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new StringVar(PreviousCardVarName, "无")];

    protected override object InitInternalData() => new Data();

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _ = applier;
        _ = cardSource;

        var data = GetInternalData<Data>();
        if (!data.initialized)
        {
            data.previousCardId = null;
            data.initialized = true;
            SyncPreviousCardVar(null);
        }

        if (Owner.Player is { } player)
        {
            SyncRemainingTriggers(player);
        }

        return Task.CompletedTask;
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

        var data = GetInternalData<Data>();
        data.previousCardId = null;
        data.remainingTriggers = System.Math.Max(0, Amount);
        data.syncedTurnNumber = Owner.Player.PlayerCombatState?.TurnNumber ?? -1;
        data.syncedAmount = Amount;
        InvokeDisplayAmountChanged();
        SyncPreviousCardVar(null);
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        _ = choiceContext;
        if (Owner.Player is not { } player || cardPlay.Card.Owner != player)
        {
            return;
        }

        var data = GetInternalData<Data>();
        var currentCardId = cardPlay.Card.Id.Entry;
        var isConsecutiveDuplicate =
            data.previousCardId is not null
            && data.previousCardId == currentCardId;

        SyncRemainingTriggers(player);
        if (isConsecutiveDuplicate && data.remainingTriggers > 0)
        {
            data.remainingTriggers--;
            InvokeDisplayAmountChanged();
            Flash();
            var refundedEnergy = cardPlay.Resources.EnergyValue;
            if (refundedEnergy > 0)
            {
                await PlayerCmd.GainEnergy(refundedEnergy, player);
            }
        }

        data.previousCardId = currentCardId;
        SyncPreviousCardVar(cardPlay.Card);
    }

    private void SyncPreviousCardVar(CardModel? card)
    {
        ((StringVar)DynamicVars[PreviousCardVarName]).StringValue =
            card?.Title ?? "无";
        InvokeDisplayAmountChanged();
    }

    private void SyncRemainingTriggers(Player player)
    {
        var data = GetInternalData<Data>();
        var currentTurnNumber = player.PlayerCombatState?.TurnNumber ?? -1;

        if (data.syncedTurnNumber != currentTurnNumber)
        {
            data.remainingTriggers = System.Math.Max(0, Amount);
            data.syncedTurnNumber = currentTurnNumber;
            data.syncedAmount = Amount;
            InvokeDisplayAmountChanged();
            return;
        }

        if (data.syncedAmount == Amount)
        {
            return;
        }

        data.remainingTriggers = System.Math.Max(
            0,
            data.remainingTriggers + Amount - data.syncedAmount);
        data.syncedAmount = Amount;
        InvokeDisplayAmountChanged();
    }
}
