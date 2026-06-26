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
public sealed class CognitionVesselRelic : KnowledgeDemonRelicModel, IKnowledgeDemonEventListener
{
    private const int KnowledgeOverloadThreshold = 9;

    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        KnowledgeDemonKeywordHoverTips.FromRecord(),
        KnowledgeDemonKeywordHoverTips.FromKnowledgeOverload(),
        KnowledgeDemonKeywordHoverTips.FromChoose(),
    ];

    public async Task AfterRecordedToLibrary(
        PlayerChoiceContext? choiceContext,
        Player player,
        CardModel sourceCard,
        IReadOnlyList<CardModel> recordedCopies)
    {
        _ = sourceCard;
        _ = recordedCopies;

        if (player != Owner)
        {
            return;
        }

        var libraryPile = BookLibraryUtility.TryGetLibraryPile(player);
        if (libraryPile is null || libraryPile.Cards.Count < KnowledgeOverloadThreshold)
        {
            return;
        }

        if (choiceContext is null)
        {
            return;
        }

        Flash();
        await BookLibraryCmd.TriggerKnowledgeOverload(choiceContext, player, this);
    }
}
