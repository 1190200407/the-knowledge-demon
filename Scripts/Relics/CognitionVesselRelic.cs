using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterCharacterStarterRelic(typeof(KnowledgeDemon))]
public sealed class CognitionVesselRelic : KnowledgeDemonRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        KnowledgeDemonKeywordHoverTips.FromRecord(),
        KnowledgeDemonKeywordHoverTips.FromChoose(),
    ];

    public override async Task BeforeFlush(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
        {
            return;
        }

        if (BookLibraryUtility.PileType.GetPile(player).Cards.Count == 0)
        {
            await BookLibraryCmd.DismissLibraryAtTurnEnd(choiceContext, player);
            return;
        }

        await BookLibraryCmd.PlayChooseStartPresentation(player);
        await BookLibraryCmd.ChooseFromLibraryAndAutoPlay(choiceContext, player);
        BookLibraryCmd.PlayChooseDonePresentation(player);
        await BookLibraryCmd.DismissLibraryAtTurnEnd(choiceContext, player);
    }
}
