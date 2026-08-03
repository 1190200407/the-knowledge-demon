using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class InfinitePower : KnowledgeDemonPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (Owner.Player != player)
        {
            return;
        }

        Flash();

        var times = (int)System.Math.Max(1m, Amount);
        for (var i = 0; i < times; i++)
        {
            var card = player.RunState.CreateCard<Infinite>(player);
            await Cmd.CustomScaledWait(0.04f, 0.1f);
            await BookLibraryCmd.RecordToLibrary(choiceContext, player, card, 1);
            await PowerCmd.Decrement(this);
        }
    }
}
