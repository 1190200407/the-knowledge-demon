using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models;

namespace ComicChess.KnowledgeDemon;

/// <summary>藏书库：入堆刷新、手动打出进弃牌堆时记录复制品。</summary>
[RegisterSingleton]
public sealed class BookLibraryPileSingleton : HookedSingletonModel
{
    public BookLibraryPileSingleton()
        : base(HookType.Combat)
    {
    }

    public override async Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        AbstractModel? clonedBy)
    {
        if (BookLibraryUtility.IsBookLibraryPile(oldPileType))
        {
            BookLibraryUtility.ResetCardTint(card);
        }

        if (card.Pile is { } enteredPile && BookLibraryUtility.IsBookLibraryPile(enteredPile.Type))
        {
            BookLibraryUtility.RefreshCardVisual(card);
        }

        await BookLibraryCmd.RecordOnEnteredDiscardPile(card, oldPileType);
    }
}
