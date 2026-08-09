using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class ZazenPower : KnowledgeDemonPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<GoodGrace>(),
    ];

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        _ = choiceContext;
        if (Owner.Player != player)
        {
            return;
        }

        Flash();
        if (player.Creature.CombatState is { } combatState)
        {
            var goodGrace = combatState.CreateCard<GoodGrace>(player);
            await CardPileCmd.AddGeneratedCardToCombat(goodGrace, PileType.Hand, player);
        }

        await PowerCmd.Decrement(this);
    }
}
