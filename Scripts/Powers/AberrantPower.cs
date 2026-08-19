using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
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

    public bool TryRedirectDiscardedStatusFromHand(CardModel card)
    {
        if (!CanRedirectDiscard(card))
        {
            return false;
        }

        ConsumeTrigger();
        Flash();
        return true;
    }

    public async Task<bool> TryRedirectDiscardedStatusFromLibrary(CardModel card)
    {
        if (!CanRedirectDiscard(card))
        {
            return false;
        }

        ConsumeTrigger();
        Flash();
        await CardPileCmd.Add(card, PileType.Hand);
        BookLibraryUtility.RefreshHandCardVisual(card);
        return true;
    }

    public override async Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
    {
        _ = choiceContext;

        if (!CanRedirectDiscard(card))
        {
            return;
        }

        ConsumeTrigger();
        Flash();
        await CardPileCmd.Add(card, PileType.Hand);
        BookLibraryUtility.RefreshHandCardVisual(card);
    }

    private bool CanRedirectDiscard(CardModel card)
    {
        if (Owner.Player is not { } player
            || card.Owner != player
            || card.Type != CardType.Status)
        {
            return false;
        }

        SyncRemainingTriggers(player);
        return GetInternalData<Data>().remainingTriggers > 0;
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

internal static class AberrantDiscardUtility
{
    public static bool TryRedirectHandDiscard(CardModel card)
    {
        if (card.Owner?.Creature?.GetPower<AberrantPower>() is not { } power)
        {
            return false;
        }

        return power.TryRedirectDiscardedStatusFromHand(card);
    }

    public static async Task<bool> TryRedirectLibraryDiscard(CardModel card)
    {
        if (card.Owner?.Creature?.GetPower<AberrantPower>() is not { } power)
        {
            return false;
        }

        return await power.TryRedirectDiscardedStatusFromLibrary(card);
    }
}
