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
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class PetrifyPower : KnowledgeDemonPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<Collapse>(),
    ];

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        _ = cardSource;

        if (target != Owner || dealer is null || !props.IsPoweredAttack() || result.BlockedDamage <= 0)
        {
            return;
        }

        Flash();
        var collapse = CombatState!.CreateCard<Collapse>(Owner.Player!);
        await CardPileCmd.AddGeneratedCardToCombat(collapse, PileType.Hand, Owner.Player!);
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        _ = choiceContext;
        _ = participants;

        if (side == CombatSide.Enemy)
        {
            await PowerCmd.Remove(this);
        }
    }
}
