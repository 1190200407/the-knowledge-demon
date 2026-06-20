using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using STS2RitsuLib.CardPiles;

namespace ComicChess.KnowledgeDemon;

/// <summary>
/// 藏书库区：摆位与索引规则对齐手牌，展示临时记录复制品。
/// </summary>
public partial class NBookLibraryPile : Control
{
    public const string ScenePath = "res://KnowledgeDemon/scenes/book_library_pile.tscn";

    private readonly Dictionary<CardModel, NBookLibraryCardHolder> _holders = [];

    /// <summary>变化时从 map 摘下的槽位，等新模型入堆时挂回（只处理这一张，不挡其它加牌）。</summary>
    private NBookLibraryCardHolder? _pendingTransformHolder;

    private Control? _dialCenter;
    private CardPile? _pile;
    private Player? _player;
    private NBookLibraryCardHolder? _focusedHolder;

    public static NBookLibraryPile? Instance { get; private set; }

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
    }

    public void Initialize(Player player)
    {
        _player = player;
        UpdateVisibility();
        AttachPile(BookLibraryUtility.PileType.GetPile(player));
    }

    public NCard? GetCard(CardModel card) => _holders.GetValueOrDefault(card)?.CardNode;

    internal NBookLibraryCardHolder? TryGetHolder(CardModel card) =>
        _holders.GetValueOrDefault(card);

    internal IReadOnlyList<NBookLibraryCardHolder> GetHolderSnapshot() =>
        _holders.Values.Where(h => GodotObject.IsInstanceValid(h)).ToList();

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
            var cardHeight = holder.IsNodeReady() && holder.Hitbox.Size.Y > 0f
                ? holder.Hitbox.Size.Y
                : NCard.defaultSize.Y;
            position.Y = (0f - cardHeight) * 0.5f + 2f;
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
