using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

/// <summary>知识寄主：与认知容器共用藏书库记录机制（非初始遗物）。</summary>
public sealed class KnowledgeHostRelic : KnowledgeDemonRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;

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

        var libraryPile = BookLibraryUtility.PileType.GetPile(player);
        if (libraryPile.Cards.Count == 0)
        {
            await BookLibraryCmd.DismissLibraryAtTurnEnd(choiceContext, player);
            return;
        }

        await BookLibraryCmd.PlayChooseStartPresentation(player);
        while (libraryPile.Cards.Count > 0 && !CombatManager.Instance.IsOverOrEnding)
        {
            await BookLibraryCmd.ChooseFromLibraryAndAutoPlay(choiceContext, player);
        }

        BookLibraryCmd.PlayChooseDonePresentation(player);
        await BookLibraryCmd.DismissLibraryAtTurnEnd(choiceContext, player);
    }
}
