using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models;

namespace ComicChess.KnowledgeDemon;

/// <summary>藏书库：入堆刷新、手动打出进弃牌堆时记录复制品。</summary>
[RegisterSingleton]
public sealed class BookLibraryPileSingleton : HookedSingletonModel
{
    private readonly Dictionary<CardModel, PlayerChoiceContext> _pendingRecordContexts = [];

    public BookLibraryPileSingleton()
        : base(HookType.Combat)
    {
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!cardPlay.IsAutoPlay
            && cardPlay.Card.Owner is Player player
            && BookLibraryUtility.PlayerHasBookLibraryRelic(player))
        {
            _pendingRecordContexts[cardPlay.Card] = choiceContext;
        }

        return Task.CompletedTask;
    }

    public override async Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        AbstractModel? clonedBy)
    {
        _ = clonedBy;

        if (BookLibraryUtility.IsBookLibraryPile(oldPileType))
        {
            BookLibraryUtility.ResetCardTint(card);
        }

        if (card.Pile is { } enteredPile && BookLibraryUtility.IsBookLibraryPile(enteredPile.Type))
        {
            BookLibraryUtility.RefreshCardVisual(card);
        }

        _pendingRecordContexts.TryGetValue(card, out var choiceContext);

        if (oldPileType == PileType.Play)
        {
            _pendingRecordContexts.Remove(card);
        }

        if (BookLibraryUtility.PlayerHasKnowledgeHostRelic(card.Owner as Player))
        {
            await BookLibraryCmd.RecordOnManualPlayResolved(choiceContext, card, oldPileType);
            return;
        }

        await BookLibraryCmd.RecordOnEnteredDiscardPile(choiceContext, card, oldPileType);
    }
}
