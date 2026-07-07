using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class LocustIncursionPower : KnowledgeDemonPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<LocustIncursion>()];

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (Owner.Player != player)
        {
            return;
        }

        Flash();
        var records = Enumerable
            .Range(0, (int)System.Math.Max(1m, Amount))
            .Select(_ => player.RunState.CreateCard<LocustIncursion>(player))
            .ToList();
        await RelativityPowerShared.RecordCardsWithDelayAsync(choiceContext, player, records);
        await Cmd.CustomScaledWait(0.5f, 1f);
        await PowerCmd.Decrement(this);
    }
}
