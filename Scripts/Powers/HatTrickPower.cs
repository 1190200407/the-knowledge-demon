using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class HatTrickPower : KnowledgeDemonPowerModel, IKnowledgeDemonEventListener
{
    private static readonly LocString MaterializePrompt =
        new("cards", "KNOWLEDGE_DEMON_CARD_HAT_TRICK.selectionScreenPrompt");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<string> RegisteredKeywordIds =>
        [KnowledgeDemonKeyword.Record, KnowledgeDemonKeyword.Materialize];

    public async Task AfterRecordedToLibrary(
        PlayerChoiceContext? choiceContext,
        Player player,
        CardModel sourceCard,
        IReadOnlyList<CardModel> recordedCopies)
    {
        _ = sourceCard;
        _ = recordedCopies;

        if (Amount <= 0 || player.Creature != Owner || choiceContext is null)
        {
            return;
        }

        Flash();
        await Cmd.CustomScaledWait(0.5f, 1f);
        await BookLibraryCmd.MaterializeFromLibraryToHand(
            choiceContext,
            player,
            1,
            MaterializePrompt,
            this);
        await PowerCmd.Decrement(this);
    }
}
