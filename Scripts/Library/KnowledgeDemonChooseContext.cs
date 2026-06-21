using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;

namespace ComicChess.KnowledgeDemon;

/// <summary>藏书库 <see cref="BookLibraryCmd.ChooseFromLibrary" /> 抉择界面生命周期。</summary>
internal static class KnowledgeDemonChooseContext
{
    private static readonly FieldInfo UpgradePreviewTypeField =
        typeof(CardModel).GetField("_upgradePreviewType", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static HashSet<CardModel>? _candidates;
    private static Action<CombatState>? _combatStateHandler;

    internal static bool IsChooseCandidate(CardModel card) => _candidates?.Contains(card) ?? false;

    /// <summary>
    /// 候选已从藏书库堆摘下，<see cref="CardModel.CombatState"/> 会变为 null；
    /// 用 <see cref="CardUpgradePreviewType.Combat"/> 对齐原版升级预览，让 hook 能读到主人战斗状态。
    /// </summary>
    internal static void Begin(IReadOnlyList<CardModel> candidates, Player player)
    {
        _candidates = candidates.ToHashSet();
        foreach (var card in candidates)
        {
            if (card.Owner is null)
            {
                Entry.Logger.Warn($"[BookLibrary][Choose] candidate {card.Id} has no owner");
            }
            else if (card.Owner != player)
            {
                Entry.Logger.Warn(
                    $"[BookLibrary][Choose] candidate {card.Id} owner mismatch: {card.Owner} vs {player}");
            }

            card.UpgradePreviewType = CardUpgradePreviewType.Combat;
        }
    }

    internal static void End()
    {
        if (_candidates != null)
        {
            foreach (var card in _candidates)
            {
                ClearChoosePreviewFlag(card);
            }
        }

        _candidates = null;
        if (_combatStateHandler != null && CombatManager.Instance is not null)
        {
            CombatManager.Instance.StateTracker.CombatStateChanged -= _combatStateHandler;
        }

        _combatStateHandler = null;
    }

    /// <summary>原版不允许通过属性从预览类型改回 None；抉择结束后必须清掉才能 AutoPlay 入堆。</summary>
    internal static void ClearChoosePreviewFlag(CardModel card) =>
        UpgradePreviewTypeField.SetValue(card, CardUpgradePreviewType.None);

    internal static void AttachChooseScreen(NChooseACardSelectionScreen screen)
    {
        if (_candidates == null)
        {
            return;
        }

        _combatStateHandler = _ => RefreshChooseScreenCards(screen);
        if (CombatManager.Instance.IsInProgress)
        {
            CombatManager.Instance.StateTracker.CombatStateChanged += _combatStateHandler;
        }

        RefreshChooseScreenCards(screen);
    }

    private static void RefreshChooseScreenCards(NChooseACardSelectionScreen screen)
    {
        foreach (var holder in screen.GetNode<Control>("CardRow").GetChildren().OfType<NGridCardHolder>())
        {
            if (holder.CardNode != null)
            {
                BookLibraryUtility.ApplyLibraryCardPreviewVisuals(holder.CardNode, applyTint: false);
            }
        }
    }
}
