using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;

namespace ComicChess.KnowledgeDemon;

/// <summary>藏书库 <see cref="BookLibraryCmd.ChooseFromLibrary" /> 抉择界面生命周期。</summary>
internal static class KnowledgeDemonChooseContext
{
    private static HashSet<CardModel>? _candidates;
    private static Action<CombatState>? _combatStateHandler;

    internal static bool IsChooseCandidate(CardModel card) => _candidates?.Contains(card) ?? false;

    internal static void Begin(IReadOnlyList<CardModel> candidates) =>
        _candidates = candidates.ToHashSet();

    internal static void End()
    {
        _candidates = null;
        if (_combatStateHandler != null && CombatManager.Instance is not null)
        {
            CombatManager.Instance.StateTracker.CombatStateChanged -= _combatStateHandler;
        }

        _combatStateHandler = null;
    }

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
