using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class GenerationalRepairPower : KnowledgeDemonPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<string> RegisteredKeywordIds =>
        [KnowledgeDemonKeyword.Record];

    public override async Task AfterCardExhausted(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool causedByEthereal)
    {
        if (Amount <= 0 || card.Owner?.Creature != Owner)
        {
            return;
        }

        if (card.Type is not CardType.Attack and not CardType.Skill)
        {
            return;
        }

        var player = Owner.Player;
        if (player is null || !BookLibraryUtility.PlayerHasBookLibraryRelic(player))
        {
            return;
        }

        Flash();
        await BookLibraryCmd.RecordToLibrary(choiceContext, player, card, 1);
        await PowerCmd.Decrement(this);
    }
}
