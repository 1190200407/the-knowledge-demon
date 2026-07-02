using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
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
    private static Action<ICombatState>? _creaturesChangedHandler;
    private static NChooseACardSelectionScreen? _screen;
    private static CombatState? _trackedCombatState;
    private static readonly HashSet<Creature> TrackedCreatures = [];

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

        ApplyPreviewTarget(null);
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
        _screen = null;
        UnsubscribeFromCreatureEvents();
        DetachCombatState();
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

        _screen = screen;
        AttachCombatState(ResolveCombatState());
        _combatStateHandler = state =>
        {
            AttachCombatState(state);
            RefreshChooseScreenCards();
        };
        if (CombatManager.Instance.IsInProgress)
        {
            CombatManager.Instance.StateTracker.CombatStateChanged += _combatStateHandler;
        }

        RefreshChooseScreenCards();
    }

    private static void RefreshChooseScreenCards()
    {
        var screen = _screen;
        if (screen == null || !GodotObject.IsInstanceValid(screen))
        {
            return;
        }

        var previewTarget = ResolvePreviewTarget();
        foreach (var holder in screen.GetNode<Control>("CardRow").GetChildren().OfType<NGridCardHolder>())
        {
            if (holder.CardNode != null)
            {
                holder.CardNode.SetPreviewTarget(previewTarget);
                BookLibraryUtility.ApplyLibraryCardPreviewVisuals(holder.CardNode, applyTint: false);
            }
        }
    }

    private static void ApplyPreviewTarget(Creature? target)
    {
        var screen = _screen;
        if (screen == null || !GodotObject.IsInstanceValid(screen))
        {
            return;
        }

        foreach (var holder in screen.GetNode<Control>("CardRow").GetChildren().OfType<NGridCardHolder>())
        {
            holder.CardNode?.SetPreviewTarget(target);
        }
    }

    private static CombatState? ResolveCombatState() =>
        _candidates?.Select(card => card.Owner?.Creature?.CombatState).OfType<CombatState>().FirstOrDefault();

    private static Creature? ResolvePreviewTarget()
    {
        var combatState = _trackedCombatState ?? ResolveCombatState();
        if (combatState == null)
        {
            return null;
        }

        var lastTargetedCreature = MegaCrit.Sts2.Core.Nodes.Rooms.NCombatRoom.Instance?.LastTargetedCreature;
        if (lastTargetedCreature != null && combatState.ContainsCreature(lastTargetedCreature) && lastTargetedCreature.IsHittable)
        {
            return lastTargetedCreature;
        }

        return combatState.HittableEnemies.FirstOrDefault();
    }

    private static void AttachCombatState(CombatState? combatState)
    {
        if (ReferenceEquals(_trackedCombatState, combatState))
        {
            SyncCreatureSubscriptions();
            return;
        }

        DetachCombatState();
        _trackedCombatState = combatState;
        if (_trackedCombatState == null)
        {
            return;
        }

        _creaturesChangedHandler = _ =>
        {
            SyncCreatureSubscriptions();
            RefreshChooseScreenCards();
        };
        _trackedCombatState.CreaturesChanged += _creaturesChangedHandler;
        SyncCreatureSubscriptions();
    }

    private static void DetachCombatState()
    {
        UnsubscribeFromCreatureEvents();
        if (_trackedCombatState != null && _creaturesChangedHandler != null)
        {
            _trackedCombatState.CreaturesChanged -= _creaturesChangedHandler;
        }

        _trackedCombatState = null;
        _creaturesChangedHandler = null;
    }

    private static void SyncCreatureSubscriptions()
    {
        var creatures = _trackedCombatState?.Creatures ?? [];
        foreach (var creature in TrackedCreatures.Except(creatures).ToList())
        {
            UnsubscribeFromCreatureEvents(creature);
            TrackedCreatures.Remove(creature);
        }

        foreach (var creature in creatures)
        {
            if (TrackedCreatures.Add(creature))
            {
                SubscribeToCreatureEvents(creature);
            }
        }
    }

    private static void SubscribeToCreatureEvents(Creature creature)
    {
        creature.BlockChanged += OnCreatureNumericChanged;
        creature.CurrentHpChanged += OnCreatureNumericChanged;
        creature.MaxHpChanged += OnCreatureNumericChanged;
        creature.PowerApplied += OnCreaturePowerApplied;
        creature.PowerIncreased += OnCreaturePowerIncreased;
        creature.PowerDecreased += OnCreaturePowerDecreased;
        creature.PowerRemoved += OnCreaturePowerRemoved;
        creature.Died += OnCreatureLifeStateChanged;
        creature.Revived += OnCreatureLifeStateChanged;
    }

    private static void UnsubscribeFromCreatureEvents()
    {
        foreach (var creature in TrackedCreatures.ToList())
        {
            UnsubscribeFromCreatureEvents(creature);
        }

        TrackedCreatures.Clear();
    }

    private static void UnsubscribeFromCreatureEvents(Creature creature)
    {
        creature.BlockChanged -= OnCreatureNumericChanged;
        creature.CurrentHpChanged -= OnCreatureNumericChanged;
        creature.MaxHpChanged -= OnCreatureNumericChanged;
        creature.PowerApplied -= OnCreaturePowerApplied;
        creature.PowerIncreased -= OnCreaturePowerIncreased;
        creature.PowerDecreased -= OnCreaturePowerDecreased;
        creature.PowerRemoved -= OnCreaturePowerRemoved;
        creature.Died -= OnCreatureLifeStateChanged;
        creature.Revived -= OnCreatureLifeStateChanged;
    }

    private static void OnCreatureNumericChanged(int _, int __) => RefreshChooseScreenCards();

    private static void OnCreaturePowerApplied(PowerModel _) => RefreshChooseScreenCards();

    private static void OnCreaturePowerIncreased(PowerModel _, int __, bool ___) => RefreshChooseScreenCards();

    private static void OnCreaturePowerDecreased(PowerModel _, bool __) => RefreshChooseScreenCards();

    private static void OnCreaturePowerRemoved(PowerModel _) => RefreshChooseScreenCards();

    private static void OnCreatureLifeStateChanged(Creature _) => RefreshChooseScreenCards();
}
