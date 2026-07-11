using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using STS2RitsuLib.CardPiles;

namespace ComicChess.KnowledgeDemon;

/// <summary>
/// 藏书库区：摆位与索引规则对齐手牌，展示临时记录复制品。
/// </summary>
public partial class NBookLibraryPile : Control
{
    public const string NodeAttachmentLocalId = "book_library_pile";
    public const string NodeAttachmentName = "BookLibraryPile";
    public const string ScenePath = "res://KnowledgeDemon/scenes/book_library_pile.tscn";
    public static readonly Vector2 DefaultPosition = Vector2.Zero;

    [Export]
    public bool EnableBottomHoverLift { get; set; } = true;

    [Export]
    public Rect2 HoverTriggerViewportRect { get; set; } = new(0f, 920f, 1920f, 160f);

    [Export]
    public float DialCenterCollapsedY { get; set; } = 1000f;

    [Export]
    public float DialCenterExpandedY { get; set; } = 950f;

    [Export]
    public float HoverLiftTweenDuration { get; set; } = 0.3f;

    [Export]
    public float FocusedCardLiftOffsetY { get; set; } = 2f;

    [Export]
    public bool AutoSizeHoverTriggerZone { get; set; } = true;

    [Export]
    public float HoverTriggerHorizontalPadding { get; set; } = 120f;

    [Export]
    public float HoverTriggerMinWidth { get; set; } = 700f;

    private readonly Dictionary<CardModel, NBookLibraryCardHolder> _holders = [];

    /// <summary>变化时从 map 摘下的槽位，等新模型入堆时挂回（只处理这一张，不挡其它加牌）。</summary>
    private NBookLibraryCardHolder? _pendingTransformHolder;

    private Control? _dialCenter;
    private Control? _hoverTriggerZone;
    private Control? _selectBackstop;
    private Control? _selectedCardContainerRoot;
    private NPlayerHand? _selectedCardHand;
    private Tween? _selectBackstopTween;
    private KnowledgeDemonCardSelectSession? _selectSession;
    private CardPile? _pile;
    private Player? _player;
    private NBookLibraryCardHolder? _focusedHolder;
    private bool _dialOrderSyncScheduled;
    private IReadOnlyList<CardModel>? _pendingDialOrderCards;
    private bool _cardsShownByToggle = true;
    private int _forcedCardsVisibleCount;
    private Tween? _hoverLiftTween;
    private bool _isBottomHoverExpanded;

    public static NBookLibraryPile? Instance { get; private set; }

    public Player? Player => _player;

    public bool AreCardsShownByToggle => _cardsShownByToggle;

    public bool AreCardsEffectivelyVisible => _forcedCardsVisibleCount > 0 || _cardsShownByToggle;

    public override void _EnterTree()
    {
        base._EnterTree();
        Instance = this;
        CombatManager.Instance.StateTracker.CombatStateChanged += OnCombatStateChanged;
    }

    public override void _ExitTree()
    {
        CombatManager.Instance.StateTracker.CombatStateChanged -= OnCombatStateChanged;
        base._ExitTree();
        if (ReferenceEquals(Instance, this))
        {
            Instance = null;
        }

        DetachPile();
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;

        _dialCenter = GetNodeOrNull<Control>("%DialCenter");
        if (_dialCenter == null)
        {
            Entry.Logger.Error("[BookLibrary] Missing %DialCenter in book_library_pile.tscn");
            return;
        }

        _dialCenter.MouseFilter = MouseFilterEnum.Ignore;
        _hoverTriggerZone = GetNodeOrNull<Control>("%HoverTriggerZone");

        _selectBackstop = GetNodeOrNull<Control>("%SelectModeBackstop");
        _selectedCardContainerRoot = GetNodeOrNull<Control>("%SelectedCardContainer");
        if (_selectBackstop == null || _selectedCardContainerRoot == null)
        {
            Entry.Logger.Error("[BookLibrary] Missing %SelectModeBackstop or %SelectedCardContainer in book_library_pile.tscn");
            return;
        }

        _selectBackstop.Visible = false;
        _selectBackstop.MouseFilter = MouseFilterEnum.Ignore;
        _selectedCardContainerRoot.Connect(Control.SignalName.FocusEntered, Callable.From(OnSelectedContainerFocus));
        ApplyCardsVisibilityState();
        UpdateHoverTriggerZoneSize();
        UpdateBottomHoverLift(force: true, immediate: true);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        _ = delta;
        UpdateHoverTriggerZoneSize();
        UpdateBottomHoverLift();
    }

    public async Task<IEnumerable<CardModel>> RunSession(
        CardSelectorPrefs prefs,
        Func<CardModel, bool> filter,
        bool includeHand)
    {
        if (_selectSession != null)
        {
            throw new InvalidOperationException("A knowledge demon card selection is already in progress.");
        }

        if (_selectBackstop == null || _selectedCardContainerRoot == null)
        {
            throw new InvalidOperationException("Book library select UI is not initialized.");
        }

        var session = new KnowledgeDemonCardSelectSession(this, prefs, filter, includeHand);
        _selectSession = session;
        try
        {
            return await session.RunAsync();
        }
        finally
        {
            _selectSession = null;
        }
    }

    internal Control? SelectedCardContainerRoot => _selectedCardContainerRoot;

    internal Control? SelectModeBackstop => _selectBackstop;

    internal void ApplySelectBackstopPeekVisibility(bool visible)
    {
        if (_selectBackstop == null || _selectSession == null)
        {
            return;
        }

        _selectBackstop.Visible = visible;
        _selectBackstop.MouseFilter = visible ? Control.MouseFilterEnum.Stop : Control.MouseFilterEnum.Ignore;
    }

    internal void BeginSelectedCardHand(NPlayerHand hand) => _selectedCardHand = hand;

    internal IReadOnlyList<NSelectedHandCardHolder> GetSelectedCardHolders() =>
        _selectedCardContainerRoot?.GetChildren().OfType<NSelectedHandCardHolder>().ToList() ?? [];

    internal void AddSelectedLibraryCard(NHandCardHolder originalHolder)
    {
        ArgumentNullException.ThrowIfNull(originalHolder);

        var container = _selectedCardContainerRoot;
        if (container == null)
        {
            return;
        }

        var cardNode = originalHolder.CardNode;
        if (cardNode is null)
        {
            return;
        }

        var globalPosition = cardNode.GlobalPosition;
        var selectedHolder = NSelectedHandCardHolder.Create(originalHolder);
        selectedHolder.Connect(
            NCardHolder.SignalName.Pressed,
            Callable.From<NCardHolder>(OnSelectedCardHolderPressed),
            (uint)ConnectFlags.Deferred);
        container.AddChildSafely(selectedHolder);
        RefreshSelectedCardPositions();
        cardNode.GlobalPosition = globalPosition;
    }

    internal void DeselectSelectedLibraryCard(CardModel card)
    {
        var holder = GetSelectedCardHolders().FirstOrDefault(h => h.CardNode?.Model == card);
        if (holder != null)
        {
            OnSelectedCardHolderPressed(holder);
        }
    }

    internal void ClearSelectedLibraryCards()
    {
        if (_selectedCardContainerRoot == null)
        {
            return;
        }

        foreach (var holder in GetSelectedCardHolders())
        {
            _selectedCardContainerRoot.RemoveChildSafely(holder);
            holder.QueueFreeSafely();
        }

        RefreshSelectedCardPositions();
        _selectedCardHand = null;
    }

    private void OnSelectedCardHolderPressed(NCardHolder holder)
    {
        var selectedHolder = (NSelectedHandCardHolder)holder;
        if (_selectedCardHand != null
            && selectedHolder.CardNode is { Model: CardModel model } cardNode)
        {
            KnowledgeDemonCardSelectSession.ActiveSession?.DeselectSharedSelectedCard(_selectedCardHand, model, cardNode);
        }

        if (_selectedCardContainerRoot != null)
        {
            _selectedCardContainerRoot.RemoveChildSafely(selectedHolder);
        }

        selectedHolder.QueueFreeSafely();
        RefreshSelectedCardPositions();
    }

    private void RefreshSelectedCardPositions()
    {
        if (_selectedCardContainerRoot == null)
        {
            return;
        }

        var holders = GetSelectedCardHolders();
        var count = holders.Count;
        _selectedCardContainerRoot.FocusMode = count > 0 ? FocusModeEnum.All : FocusModeEnum.None;
        if (count == 0)
        {
            return;
        }

        var cardWidth = holders[0].Size.X;
        var x = -cardWidth * (count - 1) / 2f;
        for (var i = 0; i < count; i++)
        {
            holders[i].Position = new Vector2(x, 0f);
            x += cardWidth;
            holders[i].FocusNeighborLeft = i > 0 ? holders[i - 1].GetPath() : holders[^1].GetPath();
            holders[i].FocusNeighborRight = i < count - 1 ? holders[i + 1].GetPath() : holders[0].GetPath();
        }
    }

    private void OnSelectedContainerFocus()
    {
        GetSelectedCardHolders().FirstOrDefault()?.TryGrabFocus();
    }

    internal void ShowSelectionUi()
    {
        if (_selectBackstop == null)
        {
            return;
        }

        _selectBackstop.Visible = true;
        _selectBackstop.MouseFilter = MouseFilterEnum.Stop;
        _selectBackstop.SelfModulate = new Color(1f, 1f, 1f, 0f);
        _selectBackstopTween?.Kill();
        _selectBackstopTween = _selectBackstop.CreateTween();
        _selectBackstopTween.TweenProperty(_selectBackstop, "self_modulate:a", 1f, 0.2f);
    }

    internal void HideSelectionUi()
    {
        if (_selectBackstop == null)
        {
            return;
        }

        _selectBackstopTween?.Kill();
        _selectBackstop.Visible = false;
        _selectBackstop.MouseFilter = MouseFilterEnum.Ignore;
        _selectBackstop.CreateTween().TweenProperty(_selectBackstop, "self_modulate:a", 0f, 0.2f);
    }

    public void Initialize(Player player)
    {
        _player = player;
        UpdateVisibility();
        AttachPile(BookLibraryUtility.PileType.GetPile(player));
        ApplyCardsVisibilityState();
        var pile = BookLibraryUtility.TryGetLibraryPile(player);
        Entry.Logger.Info(
            $"[BookLibrary][Initialize] visible={Visible} relic={BookLibraryUtility.PlayerHasBookLibraryRelic(player)} " +
            $"cards={pile?.Cards.Count ?? 0} index={GetIndex()}");
        KnowledgeDemonCardSelectSession.LogState("Initialize", this);
        UpdateHoverTriggerZoneSize();
        UpdateBottomHoverLift(force: true, immediate: true);
    }

    public NCard? GetCard(CardModel card) => _holders.GetValueOrDefault(card)?.CardNode;

    internal NBookLibraryCardHolder? TryGetHolder(CardModel card) =>
        _holders.GetValueOrDefault(card);

    internal IReadOnlyList<NBookLibraryCardHolder> GetHolderSnapshot() =>
        _holders.Values.Where(h => GodotObject.IsInstanceValid(h)).ToList();

    /// <summary>选牌：槽位摘下卡牌节点，模型仍留在藏书库堆。</summary>
    internal NCard? TakeCardForSelection(CardModel card)
    {
        if (!_holders.Remove(card, out var holder))
        {
            return null;
        }

        if (ReferenceEquals(_focusedHolder, holder))
        {
            _focusedHolder = null;
        }

        KnowledgeDemonCardSelectSession.ActiveSession?.UnregisterLibraryHolder(holder);

        var cardNode = holder.CardNode;
        holder.QueueFreeSafely();
        return cardNode;
    }

    /// <summary>取消选牌：把卡牌节点挂回表盘槽位。</summary>
    internal void RestoreSelectionVisual(CardModel card, NCard cardNode)
    {
        if (_holders.ContainsKey(card) || _dialCenter == null)
        {
            return;
        }

        var holder = NBookLibraryCardHolder.Create(cardNode);
        _holders[card] = holder;
        _dialCenter.AddChildSafely(holder);
        holder.BindCard(cardNode);
        holder.IsSelected = false;
        holder.ResetCardTransform();
        if (KnowledgeDemonCardSelectSession.ActiveSession is { IsActive: true })
        {
            holder.InSelectMode = true;
        }

        BookLibraryUtility.ApplyHandTableVisuals(cardNode);
        SyncDialHolderSiblingOrder();
        ArrangeCards(animate: true);
        KnowledgeDemonCardSelectSession.ActiveSession?.RegisterLibraryHolder(holder);
    }

    internal bool TryTakeHolderForTransform(CardModel card, out NBookLibraryCardHolder? holder)
    {
        if (!_holders.Remove(card, out holder))
        {
            holder = null;
            return false;
        }

        _pendingTransformHolder = holder;
        return true;
    }

    internal void ClearPendingTransformHolder() => _pendingTransformHolder = null;

    internal void CompleteTransformVisual(CardModel replacement, NBookLibraryCardHolder holder)
    {
        if (holder.CardNode is null)
        {
            holder.QueueFreeSafely();
            return;
        }

        _holders[replacement] = holder;
        holder.Name = $"{holder.GetType().Name}-{replacement.Id}";
        ArrangeCards(animate: true);
    }

    internal void NotifyHolderFocused(NBookLibraryCardHolder holder)
    {
        if (!AreCardsEffectivelyVisible)
        {
            return;
        }

        if (NCombatRoom.Instance?.Ui?.Hand?.InCardPlay == true)
        {
            return;
        }

        _focusedHolder = holder;
        ArrangeCards(animate: true);
    }

    internal void NotifyHolderUnfocused(NBookLibraryCardHolder holder)
    {
        if (ReferenceEquals(_focusedHolder, holder))
        {
            _focusedHolder = null;
        }

        ArrangeCards(animate: true);
    }

    private void OnCombatStateChanged(CombatState _)
    {
        if (_player == null || !LocalContext.IsMe(_player))
        {
            return;
        }

        foreach (var holder in _holders.Values)
        {
            if (GodotObject.IsInstanceValid(holder))
            {
                holder.UpdateCard();
            }
        }
    }

    public Vector2? GetSlotGlobalPosition(int pileIndex)
    {
        if (_pile == null)
        {
            return GetLayoutGlobalPosition(1, 0);
        }

        var count = _pile.Cards.Count;
        if (count <= 0)
        {
            return GetLayoutGlobalPosition(1, 0);
        }

        pileIndex = Math.Clamp(pileIndex, 0, count - 1);
        return GetLayoutGlobalPosition(count, pileIndex);
    }

    public Vector2? GetSlotGlobalPositionForCard(CardModel card)
    {
        if (_pile == null)
        {
            return GetSlotGlobalPosition(0);
        }

        var pileIndex = IndexOfCard(_pile, card);
        return pileIndex < 0 ? GetSlotGlobalPosition(0) : GetSlotGlobalPosition(pileIndex);
    }

    public void ArrangeCards(bool animate)
    {
        if (_pile == null || _dialCenter == null)
        {
            return;
        }

        var cards = _pile.Cards;
        var count = cards.Count;
        if (count <= 0)
        {
            return;
        }

        SyncDialHolderSiblingOrder(cards);

        var focusedPileIndex = GetFocusedPileIndex(cards);
        var holderScale = BookLibraryPosHelper.GetScale(count);

        for (var pileIndex = 0; pileIndex < count; pileIndex++)
        {
            var cardModel = cards[pileIndex];
            if (!_holders.TryGetValue(cardModel, out var holder) || !GodotObject.IsInstanceValid(holder))
            {
                continue;
            }

            var (targetPos, targetRot) = GetHolderLayoutTransform(holder, count, pileIndex, focusedPileIndex);
            holder.SetLibraryIndex(pileIndex + 1);
            var isFocused = ReferenceEquals(holder, _focusedHolder);

            if (!animate || !holder.IsInsideTree())
            {
                holder.StoreSlotRotation(targetRot);
                ApplyHolderLayoutInstant(holder, targetPos, targetRot, holderScale, isFocused);
                continue;
            }

            if (isFocused)
            {
                holder.SetAngleInstantly(0f);
                holder.SetScaleInstantly(Vector2.One);
                holder.Position = new Vector2(holder.Position.X, targetPos.Y);
                holder.SetTargetPosition(targetPos);
                continue;
            }

            holder.StoreSlotRotation(targetRot);
            holder.SetTargetPosition(targetPos);
            holder.SetTargetScale(holderScale);
            holder.SetTargetAngle(targetRot);
        }

        UpdateHoverTriggerZoneSize();
    }

    private void SyncDialHolderSiblingOrder()
    {
        if (_pile == null)
        {
            return;
        }

        SyncDialHolderSiblingOrder(_pile.Cards);
    }

    /// <summary>表盘内 holder 的 sibling 顺序对齐牌堆索引，避免 deselect 后叠层错乱。</summary>
    private void SyncDialHolderSiblingOrder(IReadOnlyList<CardModel> cards)
    {
        if (_dialCenter == null)
        {
            return;
        }

        _pendingDialOrderCards = cards;
        if (_dialOrderSyncScheduled)
        {
            return;
        }

        _dialOrderSyncScheduled = true;
        Callable.From(ApplyPendingDialHolderSiblingOrder).CallDeferred();
    }

    private void ApplyPendingDialHolderSiblingOrder()
    {
        _dialOrderSyncScheduled = false;
        if (_dialCenter == null || _pendingDialOrderCards == null)
        {
            return;
        }

        var cards = _pendingDialOrderCards;
        _pendingDialOrderCards = null;

        var childIndex = 0;
        for (var pileIndex = 0; pileIndex < cards.Count; pileIndex++)
        {
            if (!_holders.TryGetValue(cards[pileIndex], out var holder)
                || !GodotObject.IsInstanceValid(holder)
                || holder.GetParent() != _dialCenter)
            {
                continue;
            }

            _dialCenter.MoveChildSafely(holder, childIndex++);
        }
    }

    private void AttachPile(CardPile? pile)
    {
        if (ReferenceEquals(_pile, pile))
        {
            return;
        }

        DetachPile();
        _pile = pile;
        if (_pile == null)
        {
            return;
        }

        _pile.CardAdded += OnCardAdded;
        _pile.CardRemoved += OnCardRemoved;
        foreach (var card in _pile.Cards)
        {
            AddVisualFor(card);
        }

        ArrangeCards(animate: false);
    }

    private void DetachPile()
    {
        if (_pile == null)
        {
            return;
        }

        _pile.CardAdded -= OnCardAdded;
        _pile.CardRemoved -= OnCardRemoved;
        _pile = null;
        _focusedHolder = null;

        foreach (var holder in _holders.Values)
        {
            holder.QueueFreeSafely();
        }

        _holders.Clear();
    }

    private void OnCardAdded(CardModel card)
    {
        if (_pendingTransformHolder is { } pending && GodotObject.IsInstanceValid(pending))
        {
            CompleteTransformVisual(card, pending);
            _pendingTransformHolder = null;
            return;
        }

        AddVisualFor(card);
        ArrangeCards(animate: true);
    }

    private void OnCardRemoved(CardModel card)
    {
        if (!_holders.Remove(card, out var holder))
        {
            return;
        }

        if (ReferenceEquals(_focusedHolder, holder))
        {
            _focusedHolder = null;
        }

        holder.ResetCardTransform();
        holder.QueueFreeSafely();
        ArrangeCards(animate: true);
    }

    private void AddVisualFor(CardModel card)
    {
        if (_holders.ContainsKey(card) || _dialCenter == null)
        {
            return;
        }

        var ncard = NCard.Create(card);
        if (ncard == null)
        {
            return;
        }

        var holder = NBookLibraryCardHolder.Create(ncard);
        _holders[card] = holder;
        _dialCenter.AddChildSafely(holder);
        holder.BindCard(ncard);
        BookLibraryUtility.ApplyHandTableVisuals(ncard);
    }

    private Vector2? GetLayoutGlobalPosition(int cardCount, int pileIndex)
    {
        if (_dialCenter == null)
        {
            return null;
        }

        var pos = BookLibraryPosHelper.GetPosition(cardCount, pileIndex);
        return _dialCenter.GetGlobalTransformWithCanvas() * pos;
    }

    private int GetFocusedPileIndex(IReadOnlyList<CardModel> cards)
    {
        if (_focusedHolder?.CardNode?.Model is not { } focusedCard)
        {
            return -1;
        }

        for (var i = 0; i < cards.Count; i++)
        {
            if (cards[i] == focusedCard)
            {
                return i;
            }
        }

        return -1;
    }

    private (Vector2 position, float rotationDeg) GetHolderLayoutTransform(
        NBookLibraryCardHolder holder,
        int cardCount,
        int pileIndex,
        int focusedPileIndex)
    {
        var position = BookLibraryPosHelper.GetPosition(cardCount, pileIndex);
        position += BookLibraryPosHelper.GetHoverSpreadOffset(focusedPileIndex, pileIndex);

        if (focusedPileIndex == pileIndex)
        {
            var cardHeight = holder.CardNode is { } cardNode
                             && GodotObject.IsInstanceValid(cardNode)
                             && cardNode.IsNodeReady()
                             && cardNode.Size.Y > 0f
                ? cardNode.Size.Y
                : NCard.defaultSize.Y;
            position.Y = (0f - cardHeight) * 0.5f + FocusedCardLiftOffsetY;
        }

        var rotationDeg = BookLibraryPosHelper.GetAngle(cardCount, pileIndex);
        return (position, rotationDeg);
    }

    private static void ApplyHolderLayoutInstant(
        NBookLibraryCardHolder holder,
        Vector2 targetPos,
        float targetRot,
        Vector2 targetScale,
        bool isFocused)
    {
        holder.SetPositionInstantly(targetPos);
        if (isFocused)
        {
            holder.SetAngleInstantly(0f);
            holder.SetScaleInstantly(Vector2.One);
        }
        else
        {
            holder.SetAngleInstantly(targetRot);
            holder.SetScaleInstantly(targetScale);
        }
    }

    private void UpdateVisibility()
    {
        Visible = _player != null && BookLibraryUtility.PlayerHasBookLibraryRelic(_player);
        ApplyCardsVisibilityState();
    }

    public void ToggleCardsVisible()
    {
        _cardsShownByToggle = !_cardsShownByToggle;
        ApplyCardsVisibilityState();
    }

    internal void ForceShowCards()
    {
        _forcedCardsVisibleCount++;
        ApplyCardsVisibilityState();
    }

    internal void ReleaseForcedShowCards()
    {
        _forcedCardsVisibleCount = Math.Max(0, _forcedCardsVisibleCount - 1);
        ApplyCardsVisibilityState();
    }

    private void ApplyCardsVisibilityState()
    {
        if (_dialCenter != null)
        {
            _dialCenter.Visible = AreCardsEffectivelyVisible;
        }

        if (!AreCardsEffectivelyVisible)
        {
            _focusedHolder = null;
        }

        NLibraryPileButton.Instance?.RefreshToggleVisual();
        UpdateHoverTriggerZoneSize();
        UpdateBottomHoverLift(force: true, immediate: true);
    }

    private void UpdateHoverTriggerZoneSize()
    {
        if (!AutoSizeHoverTriggerZone
            || _hoverTriggerZone == null
            || _dialCenter == null)
        {
            return;
        }

        var halfWidth = HoverTriggerMinWidth * 0.5f;
        var validHolders = _holders.Values
            .Where(holder => GodotObject.IsInstanceValid(holder) && holder.GetParent() == _dialCenter)
            .ToList();

        if (validHolders.Count > 0)
        {
            var minX = float.PositiveInfinity;
            var maxX = float.NegativeInfinity;

            foreach (var holder in validHolders)
            {
                var cardWidth = holder.IsNodeReady() && holder.Hitbox.Size.X > 0f
                    ? holder.Hitbox.Size.X
                    : NCard.defaultSize.X;
                var cardHalfWidth = cardWidth * holder.Scale.X * 0.5f;
                minX = MathF.Min(minX, holder.Position.X - cardHalfWidth);
                maxX = MathF.Max(maxX, holder.Position.X + cardHalfWidth);
            }

            if (!float.IsInfinity(minX) && !float.IsInfinity(maxX))
            {
                halfWidth = MathF.Max(halfWidth, MathF.Max(MathF.Abs(minX), MathF.Abs(maxX)) + HoverTriggerHorizontalPadding);
            }
        }

        _hoverTriggerZone.OffsetLeft = -halfWidth;
        _hoverTriggerZone.OffsetRight = halfWidth;
    }

    private void UpdateBottomHoverLift(bool force = false, bool immediate = false)
    {
        if (_dialCenter == null)
        {
            return;
        }

        var shouldExpand = ShouldExpandDialCenterForMouse();
        if (!force && shouldExpand == _isBottomHoverExpanded)
        {
            return;
        }

        _isBottomHoverExpanded = shouldExpand;
        var target = _dialCenter.Position with
        {
            Y = shouldExpand ? DialCenterExpandedY : DialCenterCollapsedY
        };

        _hoverLiftTween?.Kill();
        if (immediate || !IsInsideTree() || HoverLiftTweenDuration <= 0f)
        {
            _dialCenter.Position = target;
            return;
        }

        _hoverLiftTween = CreateTween();
        _hoverLiftTween.TweenProperty(_dialCenter, "position", target, HoverLiftTweenDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
    }

    private bool ShouldExpandDialCenterForMouse()
    {
        if (!EnableBottomHoverLift || !Visible || !AreCardsEffectivelyVisible || _selectSession != null)
        {
            return false;
        }

        var mousePosition = GetViewport().GetMousePosition();
        if (_hoverTriggerZone != null && GodotObject.IsInstanceValid(_hoverTriggerZone))
        {
            return _hoverTriggerZone.GetGlobalRect().HasPoint(mousePosition);
        }

        return HoverTriggerViewportRect.HasPoint(mousePosition);
    }

    private static int IndexOfCard(CardPile pile, CardModel card)
    {
        for (var i = 0; i < pile.Cards.Count; i++)
        {
            if (pile.Cards[i] == card)
            {
                return i;
            }
        }

        return -1;
    }
}
