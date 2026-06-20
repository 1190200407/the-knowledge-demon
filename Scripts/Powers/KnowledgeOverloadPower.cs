using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class KnowledgeOverloadPower : KnowledgeDemonPowerModel, IKnowledgeDemonEventListener
{
    public const int LibraryOverflowThreshold = 4;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<string> RegisteredKeywordIds =>
        [KnowledgeDemonKeyword.Choose];

    public async Task AfterRecordedToLibrary(
        PlayerChoiceContext? choiceContext,
        Player player,
        CardModel sourceCard,
        IReadOnlyList<CardModel> recordedCopies)
    {
        if (choiceContext is null || Owner.Player != player)
        {
            return;
        }

        var libraryPile = BookLibraryUtility.PileType.GetPile(player);
        if (libraryPile is null || libraryPile.Cards.Count <= LibraryOverflowThreshold)
        {
            return;
        }

        Flash();
        await BookLibraryCmd.ChooseFromLibraryAndAutoPlay(choiceContext, player);
    }
}
