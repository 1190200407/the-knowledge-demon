using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ComicChess.KnowledgeDemon;

/// <summary>
/// 藏书库选牌会话：遮罩/表盘在 pile；选中预览在 pile 中间区；Confirm/Peek 用手牌节点。
/// </summary>
internal sealed class KnowledgeDemonCardSelectSession
{
    private const BindingFlags InstanceAny = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly FieldInfo SelectedCardsField =
        typeof(NPlayerHand).GetField("_selectedCards", InstanceAny)!;

    private static readonly FieldInfo PrefsField =
        typeof(NPlayerHand).GetField("_prefs", InstanceAny)!;

    private static readonly FieldInfo SelectionCompletionSourceField =
        typeof(NPlayerHand).GetField("_selectionCompletionSource", InstanceAny)!;

    private static readonly FieldInfo CurrentSelectionFilterField =
        typeof(NPlayerHand).GetField("_currentSelectionFilter", InstanceAny)!;

    private static readonly FieldInfo CurrentModeField =
        typeof(NPlayerHand).GetField("_currentMode", InstanceAny)!;

    private static readonly FieldInfo IsDisabledField =
        typeof(NPlayerHand).GetField("_isDisabled", InstanceAny)!;

    private static readonly MethodInfo GetHandInsertIndexMethod =
        typeof(NPlayerHand).GetMethod("GetHandInsertIndex", InstanceAny)!;

    private static readonly MethodInfo AfterCardsSelectedMethod =
        typeof(NPlayerHand).GetMethod("AfterCardsSelected", InstanceAny)!;

    private static readonly MethodInfo UpdateSelectModeCardVisibilityMethod =
        typeof(NPlayerHand).GetMethod("UpdateSelectModeCardVisibility", InstanceAny)!;

    private static readonly MethodInfo RefreshSelectModeConfirmButtonMethod =
        typeof(NPlayerHand).GetMethod("RefreshSelectModeConfirmButton", InstanceAny)!;

    private static readonly MethodInfo AnimEnableMethod =
        typeof(NPlayerHand).GetMethod("AnimEnable", InstanceAny)!;

    private static readonly MethodInfo AnimDisableMethod =
        typeof(NPlayerHand).GetMethod("AnimDisable", InstanceAny)!;

    private readonly NBookLibraryPile _libraryPile;
    private readonly CardSelectorPrefs _prefs;
    private readonly Func<CardModel, bool> _filter;
    private readonly bool _includeHand;
    private readonly HashSet<CardModel> _libraryOriginCards = [];
    private readonly HashSet<CardModel> _sharedHandOriginCards = [];
    private readonly List<NBookLibraryCardHolder> _libraryHolders = [];
    private readonly Callable _libraryHolderPressedCallable;

    private int _savedLibraryIndex = -1;
    private bool _vanillaCleanupDone;
    private int _libraryConfirmRefreshVersion;
    private bool _isForcingLibraryVisible;

    internal static KnowledgeDemonCardSelectSession? ActiveSession { get; private set; }

    internal bool IncludeHand => _includeHand;

    internal bool IsActive => ActiveSession == this;

    public KnowledgeDemonCardSelectSession(
        NBookLibraryPile libraryPile,
        CardSelectorPrefs prefs,
        Func<CardModel, bool> filter,
        bool includeHand)
    {
        _libraryPile = libraryPile;
        _prefs = prefs;
        _filter = filter;
        _includeHand = includeHand;
        _libraryHolderPressedCallable = Callable.From<NCardHolder>(OnLibraryHolderPressed);
    }

    public async Task<IEnumerable<CardModel>> RunAsync()
    {
        EnterLayerSetup();
        try
        {
            var hand = NCombatRoom.Instance!.Ui!.Hand;
            return await RunVanillaHandSimpleSelectAsync(hand);
        }
        finally
        {
            ExitLayerSetup();
        }
    }

    internal void MarkLibraryOrigin(CardModel card) => _libraryOriginCards.Add(card);

    internal bool IsLibraryOrigin(CardModel card) => _libraryOriginCards.Contains(card);

    internal void MarkSharedHandOrigin(CardModel card) => _sharedHandOriginCards.Add(card);

    internal bool IsSharedHandOrigin(CardModel card) => _sharedHandOriginCards.Contains(card);

    internal static List<CardModel> GetSelectedCards(NPlayerHand hand) =>
        (List<CardModel>)SelectedCardsField.GetValue(hand)!;

    private static int GetHandInsertIndex(NPlayerHand hand, CardModel card) =>
        (int)GetHandInsertIndexMethod.Invoke(hand, [card])!;

    internal static void RefreshHandConfirmButton(NPlayerHand hand)
    {
        if (!hand.IsInCardSelection)
        {
            return;
        }

        RefreshSelectModeConfirmButtonMethod.Invoke(hand, null);
    }

    private void ScheduleLibraryConfirmRefresh(NPlayerHand hand)
    {
        var version = ++_libraryConfirmRefreshVersion;
        Callable.From(() =>
        {
            if (ActiveSession != this || version != _libraryConfirmRefreshVersion)
            {
                return;
            }

            RefreshHandConfirmButton(hand);
        }).CallDeferred();
    }

    private void CancelPendingLibraryConfirmRefresh() => _libraryConfirmRefreshVersion++;

    private static void HideSelectModeConfirmButton(NPlayerHand hand) =>
        hand.GetNode<NConfirmButton>("%SelectModeConfirmButton").Disable();

    internal void DeselectSharedSelectedCard(NPlayerHand hand, CardModel model, NCard cardNode)
    {
        if (IsLibraryOrigin(model))
        {
            GetSelectedCards(hand).Remove(model);
            _libraryOriginCards.Remove(model);
            RefreshHandConfirmButton(hand);
            _libraryPile.RestoreSelectionVisual(model, cardNode);
            return;
        }

        if (!IsSharedHandOrigin(model))
        {
            return;
        }

        _sharedHandOriginCards.Remove(model);
        hand.DeselectCard(cardNode);
    }

    internal void RestoreAllLibrarySelections()
    {
        var hand = NCombatRoom.Instance?.Ui?.Hand;
        foreach (var holder in _libraryPile.GetSelectedCardHolders())
        {
            if (holder.CardNode is not { Model: CardModel model } cardNode || hand == null)
            {
                continue;
            }

            if (IsLibraryOrigin(model))
            {
                GetSelectedCards(hand).Remove(model);
                _libraryOriginCards.Remove(model);
                _libraryPile.RestoreSelectionVisual(model, cardNode);
                continue;
            }

            if (IsSharedHandOrigin(model))
            {
                RestoreSharedHandSelection(hand, model, cardNode);
            }
        }

        _libraryPile.ClearSelectedLibraryCards();
    }

    internal void SelectHandCard(NPlayerHand hand, NHandCardHolder holder)
    {
        if (holder.CardNode?.Model is not CardModel model)
        {
            return;
        }

        if (hand.PeekButton.IsPeeking)
        {
            hand.PeekButton.Wiggle();
            return;
        }

        _libraryPile.BeginSelectedCardHand(hand);

        var selectedCards = GetSelectedCards(hand);
        var prefs = (CardSelectorPrefs)PrefsField.GetValue(hand)!;
        if (selectedCards.Count >= prefs.MaxSelect)
        {
            DeselectSelectedCard(hand, selectedCards[^1]);
        }

        _libraryPile.AddSelectedLibraryCard(holder);
        hand.RemoveCardHolder(holder);
        selectedCards.Add(model);
        MarkSharedHandOrigin(model);
        ScheduleLibraryConfirmRefresh(hand);
        LogState("SelectHandCard");
    }

    internal void RegisterLibraryHolder(NBookLibraryCardHolder holder)
    {
        _libraryHolders.RemoveAll(h => !GodotObject.IsInstanceValid(h));

        if (_libraryHolders.Contains(holder))
        {
            return;
        }

        holder.Connect(NCardHolder.SignalName.Pressed, _libraryHolderPressedCallable);
        _libraryHolders.Add(holder);
        holder.InSelectMode = true;
        if (holder.CardNode?.Model is CardModel card)
        {
            holder.Visible = _filter(card);
        }

        holder.UpdateCard();
    }

    internal void UnregisterLibraryHolder(NBookLibraryCardHolder holder)
    {
        if (!_libraryHolders.Remove(holder))
        {
            return;
        }

        if (!GodotObject.IsInstanceValid(holder))
        {
            return;
        }

        holder.Disconnect(NCardHolder.SignalName.Pressed, _libraryHolderPressedCallable);
        holder.InSelectMode = false;
        holder.IsSelected = false;
    }

    internal void RefreshLibraryHolderVisibility()
    {
        foreach (var holder in _libraryHolders)
        {
            if (!GodotObject.IsInstanceValid(holder) || holder.CardNode?.Model is not CardModel card)
            {
                continue;
            }

            holder.Visible = _filter(card);
            holder.UpdateCard();
        }
    }

    private void OnLibraryHolderPressed(NCardHolder holder)
    {
        if (holder is not NBookLibraryCardHolder libraryHolder
            || libraryHolder.CardNode?.Model is not CardModel card
            || !_filter(card))
        {
            return;
        }

        var hand = NCombatRoom.Instance?.Ui?.Hand;
        if (hand == null)
        {
            return;
        }

        if (hand.PeekButton.IsPeeking)
        {
            hand.PeekButton.Wiggle();
            return;
        }

        var cardNode = NBookLibraryPile.Instance?.TakeCardForSelection(card);
        if (cardNode == null)
        {
            Entry.Logger.Warn($"[BookLibrary][Select] TakeCardForSelection failed for {card.Id}");
            return;
        }

        SelectLibraryCard(hand, cardNode, card);
    }

    private void SelectLibraryCard(NPlayerHand hand, NCard cardNode, CardModel model)
    {
        if (hand.PeekButton.IsPeeking)
        {
            hand.PeekButton.Wiggle();
            return;
        }

        _libraryPile.BeginSelectedCardHand(hand);

        var selectedCards = GetSelectedCards(hand);
        var prefs = (CardSelectorPrefs)PrefsField.GetValue(hand)!;
        if (selectedCards.Count >= prefs.MaxSelect)
        {
            DeselectSelectedCard(hand, selectedCards[^1]);
        }

        var tempHolder = NHandCardHolder.Create(cardNode, hand);
        tempHolder.InSelectMode = true;
        _libraryPile.AddSelectedLibraryCard(tempHolder);
        tempHolder.QueueFreeSafely();
        selectedCards.Add(model);
        MarkLibraryOrigin(model);
        ScheduleLibraryConfirmRefresh(hand);
        LogState("SelectLibraryCard");
    }

    private void DeselectSelectedCard(NPlayerHand hand, CardModel model)
    {
        if (IsLibraryOrigin(model) || IsSharedHandOrigin(model))
        {
            _libraryPile.DeselectSelectedLibraryCard(model);
            return;
        }

        hand.GetNode<NSelectedHandCardContainer>("%SelectedHandCardContainer").DeselectCard(model);
    }

    internal void RevalidateSelectionAfterStateChange(NPlayerHand hand)
    {
        if (CurrentSelectionFilterField.GetValue(hand) is not Func<CardModel, bool> filter)
        {
            return;
        }

        foreach (var holder in _libraryPile.GetSelectedCardHolders().ToList())
        {
            var cardModel = holder.CardNode?.Model;
            if (cardModel != null && !filter(cardModel))
            {
                _libraryPile.DeselectSelectedLibraryCard(cardModel);
            }
        }

        UpdateSelectModeCardVisibilityMethod.Invoke(hand, null);
        RefreshLibraryHolderVisibility();

        var hasHandCandidate = hand.CardHolderContainer
            .GetChildren()
            .OfType<NHandCardHolder>()
            .Any(h => h.CardNode?.Model is CardModel card && filter(card));
        var hasLibraryCandidate = _libraryPile
            .GetHolderSnapshot()
            .Any(h => h.CardNode?.Model is CardModel card && filter(card));

        if (GetSelectedCards(hand).Count == 0 && !hasHandCandidate && !hasLibraryCandidate)
        {
            if (SelectionCompletionSourceField.GetValue(hand) is TaskCompletionSource<IEnumerable<CardModel>> tcs)
            {
                tcs.TrySetResult([]);
            }
        }
        else
        {
            RefreshHandConfirmButton(hand);
        }
    }

    /// <summary>
    /// 接原版手牌 SimpleSelect：OnHandSelectModeEntered 抬手牌 UI；纯藏书库默认隐藏 CardHolderContainer，
    /// 但在 Peek 时重新显示，方便查看手牌。
    /// </summary>
    private async Task<IEnumerable<CardModel>> RunVanillaHandSimpleSelectAsync(NPlayerHand hand)
    {
        ActiveSession = this;
        var handHeader = hand.GetNode<MegaRichTextLabel>("%SelectionHeader");
        var handBackstop = hand.GetNode<Control>("%SelectModeBackstop");
        var libraryContainer = _libraryPile.SelectedCardContainerRoot
            ?? throw new InvalidOperationException("Book library selected card container is not ready.");
        _libraryPile.BeginSelectedCardHand(hand);
        LogState("SelectBegin");
        var peekToggledCallable = Callable.From<NPeekButton>(OnPeekButtonToggled);
        hand.PeekButton.Connect(NPeekButton.SignalName.Toggled, peekToggledCallable);
        try
        {
            hand.CancelAllCardPlay();

            var wasDisabled = (bool)IsDisabledField.GetValue(hand)!;
            if (wasDisabled)
            {
                AnimEnableMethod.Invoke(hand, null);
            }

            CurrentModeField.SetValue(hand, NPlayerHand.Mode.SimpleSelect);
            CurrentSelectionFilterField.SetValue(hand, _filter);
            PrefsField.SetValue(hand, _prefs);

            NCombatRoom.Instance!.RestrictControllerNavigation([]);
            NCombatRoom.Instance.Ui.OnHandSelectModeEntered();

            hand.EnableControllerNavigation();
            if (_libraryPile.SelectModeBackstop != null)
            {
                hand.PeekButton.AddTargets(libraryContainer, _libraryPile.SelectModeBackstop);
            }
            else
            {
                hand.PeekButton.AddTargets(libraryContainer);
            }

            var tcs = new TaskCompletionSource<IEnumerable<CardModel>>();
            SelectionCompletionSourceField.SetValue(hand, tcs);

            handHeader.Visible = true;
            handHeader.Text = "[center]" + _prefs.Prompt.GetFormattedText() + "[/center]";
            handBackstop.Visible = false;
            handBackstop.MouseFilter = Control.MouseFilterEnum.Ignore;

            hand.PeekButton.Enable();
            UpdateSelectModeCardVisibilityMethod.Invoke(hand, null);
            RefreshLibraryHolderVisibility();
            RefreshHandConfirmButton(hand);

            ApplyHandVisibility(hand, hand.PeekButton.IsPeeking);

            LogState("SelectReady");

            try
            {
                IEnumerable<CardModel> result;
                try
                {
                    result = await tcs.Task;
                }
                catch (OperationCanceledException)
                {
                    result = [];
                    _vanillaCleanupDone = true;
                }

                FinishHandSelection(hand, source: null, wasDisabled);

                NCombatRoom.Instance?.EnableControllerNavigation();
                LogState("SelectComplete");
                return result;
            }
            finally
            {
                hand.CardHolderContainer.Visible = true;

                handHeader.Visible = false;
                CancelPendingLibraryConfirmRefresh();
                HideSelectModeConfirmButton(hand);
            }
        }
        finally
        {
            if (GodotObject.IsInstanceValid(hand.PeekButton))
            {
                hand.PeekButton.Disconnect(NPeekButton.SignalName.Toggled, peekToggledCallable);
            }

            ActiveSession = null;
            LogState("SelectEnd");
        }
    }

    private void OnPeekButtonToggled(NPeekButton button)
    {
        var hand = NCombatRoom.Instance?.Ui?.Hand;
        if (hand != null)
        {
            ApplyHandVisibility(hand, button.IsPeeking);
        }

        RefreshLibraryHolderVisibilityForPeek(button.IsPeeking);
        _libraryPile.ApplySelectBackstopPeekVisibility(!button.IsPeeking);
    }

    private void ApplyHandVisibility(NPlayerHand hand, bool isPeeking)
    {
        if (_includeHand)
        {
            hand.CardHolderContainer.Visible = true;
            return;
        }

        hand.CardHolderContainer.Visible = isPeeking;
    }

    private void RefreshLibraryHolderVisibilityForPeek(bool isPeeking)
    {
        foreach (var holder in _libraryHolders)
        {
            if (!GodotObject.IsInstanceValid(holder) || holder.CardNode?.Model is not CardModel card)
            {
                continue;
            }

            holder.Visible = isPeeking || _filter(card);
            holder.UpdateCard();
        }
    }

    private void FinishHandSelection(NPlayerHand hand, AbstractModel? source, bool wasDisabled)
    {
        CancelPendingLibraryConfirmRefresh();
        RestoreAllLibrarySelections();
        if (!_vanillaCleanupDone)
        {
            AfterCardsSelectedMethod.Invoke(hand, [source]);
        }

        SelectionCompletionSourceField.SetValue(hand, null);
        HideSelectModeConfirmButton(hand);

        if (wasDisabled)
        {
            AnimDisableMethod.Invoke(hand, null);
        }
    }

    private void EnterLayerSetup()
    {
        var ui = NCombatRoom.Instance!.Ui!;
        var library = _libraryPile;

        library.ForceShowCards();
        _isForcingLibraryVisible = true;
        _savedLibraryIndex = library.GetIndex();
        // PlayContainer 在 combat_ui 里排在 Hand 之后；抬到其上方才能让 backstop 盖住已打出牌。
        var layerIndex = ui.PlayContainer.GetIndex() + 1;
        ui.MoveChildSafely(library, layerIndex);
        library.ShowSelectionUi();
        library.ArrangeCards(animate: false);

        CollectLibraryHolders();
        NCombatRoom.Instance?.RestrictControllerNavigation([]);
        LogState("EnterLayer");
    }

    private void ExitLayerSetup()
    {
        var ui = NCombatRoom.Instance!.Ui!;

        RestoreAllLibrarySelections();

        foreach (var holder in _libraryHolders)
        {
            if (!GodotObject.IsInstanceValid(holder))
            {
                continue;
            }

            holder.Disconnect(NCardHolder.SignalName.Pressed, _libraryHolderPressedCallable);
            holder.InSelectMode = false;
            holder.IsSelected = false;
            holder.Visible = true;
            holder.UpdateCard();
        }

        _libraryHolders.Clear();
        _libraryOriginCards.Clear();
        _sharedHandOriginCards.Clear();

        _libraryPile.HideSelectionUi();
        if (_isForcingLibraryVisible)
        {
            _libraryPile.ReleaseForcedShowCards();
            _isForcingLibraryVisible = false;
        }

        if (_savedLibraryIndex >= 0)
        {
            ui.MoveChildSafely(_libraryPile, _savedLibraryIndex);
            _savedLibraryIndex = -1;
            _libraryPile.ArrangeCards(animate: false);
        }

        NCombatRoom.Instance?.EnableControllerNavigation();
        var hand = NCombatRoom.Instance?.Ui?.Hand;
        if (hand != null)
        {
            HideSelectModeConfirmButton(hand);
        }

        LogState("ExitLayer");
    }

    private void RestoreSharedHandSelection(NPlayerHand hand, CardModel model, NCard cardNode)
    {
        _sharedHandOriginCards.Remove(model);
        if (hand.IsInCardSelection)
        {
            hand.DeselectCard(cardNode);
            return;
        }

        GetSelectedCards(hand).Remove(model);
        var holder = hand.Add(cardNode, GetHandInsertIndex(hand, model));
        holder.InSelectMode = false;
        holder.Visible = true;
    }

    private void CollectLibraryHolders()
    {
        foreach (var holder in _libraryPile.GetHolderSnapshot())
        {
            RegisterLibraryHolder(holder);
        }
    }

    internal static void LogState(string phase, NBookLibraryPile? pile = null)
    {
        pile ??= NBookLibraryPile.Instance;
        if (pile == null)
        {
            Entry.Logger.Info($"[BookLibrary][{phase}] pile=null");
            return;
        }

        var ui = NCombatRoom.Instance?.Ui;
        var hand = ui?.Hand;
        Entry.Logger.Info(
            $"[BookLibrary][{phase}] pileVisible={pile.Visible} pileIndex={pile.GetIndex()} " +
            $"holders={pile.GetHolderSnapshot().Count} " +
            $"handIndex={hand?.GetIndex() ?? -1} includeHand={ActiveSession?.IncludeHand} " +
            $"sessionActive={ActiveSession != null}");
    }
}
