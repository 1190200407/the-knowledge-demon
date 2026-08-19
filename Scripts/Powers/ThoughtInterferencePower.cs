using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class ThoughtInterferencePower : KnowledgeDemonPowerModel, IKnowledgeDemonEventListener
{
    private sealed class Data
    {
        public int choicesLeft;
    }

    private const string ChoicesLeftKey = "ChoicesLeft";

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => GetInternalData<Data>().choicesLeft;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar(ChoicesLeftKey, 1m)];

    protected override object InitInternalData() => new Data();

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _ = applier;
        _ = cardSource;
        ResetChoicesLeft();
        return Task.CompletedTask;
    }

    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        _ = combatState;
        if (side != CombatSide.Player || !participants.Contains(Owner))
        {
            return Task.CompletedTask;
        }

        ResetChoicesLeft();
        return Task.CompletedTask;
    }

    public Task<int> ModifyChosenCardPlayCount(
        PlayerChoiceContext choiceContext,
        Player player,
        CardModel? chooseSource,
        CardModel chosenCard,
        int playCount)
    {
        _ = choiceContext;
        _ = chooseSource;

        if (player != Owner.Player || chosenCard.Owner != Owner.Player || GetInternalData<Data>().choicesLeft <= 0)
        {
            return Task.FromResult(playCount);
        }

        Flash();
        GetInternalData<Data>().choicesLeft--;
        InvokeDisplayAmountChanged();
        return Task.FromResult(playCount + 1);
    }

    private void ResetChoicesLeft()
    {
        GetInternalData<Data>().choicesLeft = Math.Max(0, Amount);
        InvokeDisplayAmountChanged();
    }
}
