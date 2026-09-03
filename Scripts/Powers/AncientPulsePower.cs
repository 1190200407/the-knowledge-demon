using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class AncientPulsePower : KnowledgeDemonPowerModel
{
    private sealed class Data
    {
        public CardModel? SourceCard;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override object InitInternalData() => new Data();

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _ = applier;
        GetInternalData<Data>().SourceCard = cardSource;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player || ReferenceEquals(cardPlay.Card, GetInternalData<Data>().SourceCard))
        {
            return;
        }

        if (CombatState is not { HittableEnemies.Count: > 0 } combatState)
        {
            return;
        }

        Flash();
        await CreatureCmd.Damage(
            choiceContext,
            combatState.HittableEnemies,
            Amount,
            ValueProp.Unpowered,
            Owner,
            null,
            null);
    }
}
