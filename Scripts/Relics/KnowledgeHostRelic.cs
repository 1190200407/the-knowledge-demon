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
using STS2RitsuLib.Interactions.RightClick;

namespace ComicChess.KnowledgeDemon;

/// <summary>知识寄主：与认知容器共用藏书库记录机制（非初始遗物）。</summary>
public sealed class KnowledgeHostRelic : KnowledgeDemonRelicModel, IKnowledgeDemonEventListener, IModRightClickableRelic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        KnowledgeDemonKeywordHoverTips.FromRecord(),
        KnowledgeDemonKeywordHoverTips.FromKnowledgeOverloadOmega(),
        KnowledgeDemonKeywordHoverTips.FromChooseOmega(),
    ];

    public bool CanHandleRightClickLocal(ModRightClickContext context) =>
        CombatManager.Instance.IsInProgress;

    public bool CanExecuteRightClick(ModRightClickExecutionContext context) =>
        CombatManager.Instance.IsInProgress;

    public Task OnRightClick(ModRightClickExecutionContext context)
    {
        _ = context;
        if (!CombatManager.Instance.IsInProgress)
        {
            return Task.CompletedTask;
        }

        return KnowledgeDemonRuleFtueBootstrap.ShowRuleFtueAsync(NKnowledgeDemonRuleFtue.RuleType.KnowledgeDemon);
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
