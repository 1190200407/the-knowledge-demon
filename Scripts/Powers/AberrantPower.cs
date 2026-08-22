using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class AberrantPower : KnowledgeDemonPowerModel
{
    private sealed class Data
    {
        public int remainingTriggers;
        public int syncedTurnNumber = -1;
        public int syncedAmount;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => GetInternalData<Data>().remainingTriggers;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromKeyword(CardKeyword.Sly)];

    protected override object InitInternalData() => new Data();

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _ = applier;
        _ = cardSource;
        ResetSyncState();
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

        ResetSyncState();
        SyncRemainingTriggers(Owner.Player);
        return Task.CompletedTask;
    }

    public override Task AfterCardGeneratedForCombat(
        CardModel card,
        Player? creator)
    {
        if (creator != Owner.Player
            || card.Owner != Owner.Player
            || card.Type != CardType.Status)
        {
            return Task.CompletedTask;
        }

        SyncRemainingTriggers(Owner.Player);
        var data = GetInternalData<Data>();
        if (data.remainingTriggers <= 0)
        {
            return Task.CompletedTask;
        }

        ConsumeTrigger();
        Flash();
        if (!card.Keywords.Contains(CardKeyword.Sly))
        {
            card.AddKeyword(CardKeyword.Sly);
        }

        return Task.CompletedTask;
    }

    private void ConsumeTrigger()
    {
        var data = GetInternalData<Data>();
        data.remainingTriggers = Math.Max(0, data.remainingTriggers - 1);
        InvokeDisplayAmountChanged();
    }

    private void ResetSyncState()
    {
        var data = GetInternalData<Data>();
        data.syncedTurnNumber = -1;
        data.syncedAmount = 0;
        data.remainingTriggers = 0;
        InvokeDisplayAmountChanged();
    }

    private void SyncRemainingTriggers(Player player)
    {
        var data = GetInternalData<Data>();
        var currentTurnNumber = player.PlayerCombatState?.TurnNumber ?? -1;
        var changed = false;

        if (data.syncedTurnNumber != currentTurnNumber)
        {
            data.remainingTriggers = Math.Max(0, Amount);
            data.syncedTurnNumber = currentTurnNumber;
            data.syncedAmount = Amount;
            changed = true;
        }
        else if (data.syncedAmount != Amount)
        {
            var delta = Amount - data.syncedAmount;
            data.remainingTriggers = Math.Max(0, data.remainingTriggers + delta);
            data.syncedAmount = Amount;
            changed = true;
        }

        if (changed)
        {
            InvokeDisplayAmountChanged();
        }
    }
}
