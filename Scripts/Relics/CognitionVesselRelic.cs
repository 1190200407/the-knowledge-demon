using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterCharacterStarterRelic(typeof(KnowledgeDemon))]
public sealed class CognitionVesselRelic : KnowledgeDemonRelicModel, IKnowledgeDemonEventListener
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        KnowledgeDemonKeywordHoverTips.FromRecord(),
        KnowledgeDemonKeywordHoverTips.FromKnowledgeOverload(),
        KnowledgeDemonKeywordHoverTips.FromChoose(),
    ];

    public override Task AfterObtained()
    {
        BookLibraryUtility.RefreshLocalBookLibraryUi(Owner);
        return Task.CompletedTask;
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        _ = room;

        if (!IsWax)
        {
            return;
        }

        var owner = Owner;
        await RelicCmd.Remove(this);
        BookLibraryUtility.RefreshLocalBookLibraryUi(owner);
    }

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

        await BookLibraryCmd.TryTriggerKnowledgeOverloadIfThresholdReached(choiceContext, player, this);
    }
}
