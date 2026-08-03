using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Cards;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;

namespace ComicChess.KnowledgeDemon;

/// <summary>
/// 藏书库槽位 Holder：摆位/缩放/旋转动画对齐 <see cref="NHandCardHolder" />。
/// </summary>
public partial class NBookLibraryCardHolder : NCardHolder
{
    public const string ScenePath = "res://KnowledgeDemon/scenes/book_library_card_holder.tscn";

    private const float RotateSpeed = 10f;
    private const float ScaleSpeed = 8f;
    private const float MoveSpeed = 7f;
    private const float AngleSnapThreshold = 0.1f;
    private const float ScaleSnapThreshold = 0.002f;
    private const float PositionSnapThreshold = 1f;

    private Control? _flash;
    private MegaLabel? _libraryIndexLabel;
    private Tween? _flashTween;
    private float _slotRotationDeg;

    private Vector2 _targetPosition;
    private float _targetAngle;
    private Vector2 _targetScale;

    private CancellationTokenSource? _angleCancelToken;
    private CancellationTokenSource? _positionCancelToken;
    private CancellationTokenSource? _scaleCancelToken;

    public bool InSelectMode { get; set; }

    public bool IsSelected { get; set; }

    public float LayoutRotationDegrees => _isFocused ? 0f : _slotRotationDeg;

    public static NBookLibraryCardHolder Create(NCard card)
    {
        if (!ResourceLoader.Exists(ScenePath))
        {
            Entry.Logger.Error($"[BookLibrary] Holder scene missing: {ScenePath}");
            throw new InvalidOperationException($"Missing library holder scene: {ScenePath}");
        }

        var holder = ResourceLoader.Load<PackedScene>(ScenePath)
            .Instantiate<NBookLibraryCardHolder>(PackedScene.GenEditState.Disabled);
        holder.Name = $"{holder.GetType().Name}-{card.Model!.Id}";
        return holder;
    }

    public void BindCard(NCard card) => SetCard(card);

    public override void _Ready()
    {
        ConnectSignals();
        _flash = GetNodeOrNull<Control>("Flash");
        if (_flash != null)
        {
            _flash.Modulate = new Color(_flash.Modulate.R, _flash.Modulate.G, _flash.Modulate.B, 0f);
        }

        _libraryIndexLabel = GetNode<MegaLabel>("%LibraryIndex");
        Scale = SmallScale;
        UpdateCard();

        if (Hitbox != null)
        {
            Hitbox.SetEnabled(enabled: true);
        }
    }

    public override void _ExitTree()
    {
        DetachCardNodeEvents();
        base._ExitTree();
        UnsubscribeFromEvents(CardNode?.Model);
        StopAnimations();
    }

    public override void Clear()
    {
        DetachCardNodeEvents();
        UnsubscribeFromEvents(CardNode?.Model);
        base.Clear();
        StopAnimations();
    }

    protected override void SetCard(NCard node)
    {
        if (CardNode != null)
        {
            CardNode.ModelChanged -= OnModelChanged;
        }

        UnsubscribeFromEvents(CardNode?.Model);
        base.SetCard(node);
        ResetCardTransform();
        SubscribeToEvents(CardNode?.Model);
        if (CardNode != null)
        {
            CardNode.ModelChanged += OnModelChanged;
        }

        if (node.Scale != Vector2.One)
        {
            node.CreateTween().TweenProperty(node, "scale", Vector2.One, 0.25);
        }

        if (IsNodeReady())
        {
            UpdateCard();
        }
    }

    protected override void OnFocus()
    {
        if (IsHandDraggingCard())
        {
            return;
        }

        NBookLibraryPile.Instance?.NotifyHolderFocused(this);
        base.OnFocus();
    }

    protected override void OnUnfocus()
    {
        base.OnUnfocus();
        NBookLibraryPile.Instance?.NotifyHolderUnfocused(this);
    }

    public void ResetCardTransform()
    {
        if (CardNode == null || !GodotObject.IsInstanceValid(CardNode))
        {
            return;
        }

        CardNode.Position = Vector2.Zero;
        CardNode.RotationDegrees = 0f;
        CardNode.Scale = Vector2.One;
    }

    public void StoreSlotRotation(float rotationDeg) => _slotRotationDeg = rotationDeg;

    public void SetSlotRotation(float rotationDeg)
    {
        StoreSlotRotation(rotationDeg);
        RotationDegrees = LayoutRotationDegrees;
    }

    public void SetTargetAngle(float angle)
    {
        if (_isFocused)
        {
            return;
        }

        _targetAngle = angle;
        _angleCancelToken?.Cancel();
        _angleCancelToken = new CancellationTokenSource();
        TaskHelper.RunSafely(AnimAngle(_angleCancelToken));
    }

    public void SetTargetPosition(Vector2 position)
    {
        _targetPosition = position;
        _positionCancelToken?.Cancel();
        _positionCancelToken = new CancellationTokenSource();
        TaskHelper.RunSafely(AnimPosition(_positionCancelToken));
    }

    public void SetTargetScale(Vector2 scale)
    {
        if (_isFocused)
        {
            return;
        }

        _targetScale = scale;
        _scaleCancelToken?.Cancel();
        _scaleCancelToken = new CancellationTokenSource();
        TaskHelper.RunSafely(AnimScale(_scaleCancelToken));
    }

    public void SetAngleInstantly(float angle)
    {
        _angleCancelToken?.Cancel();
        RotationDegrees = angle;
    }

    public void SetScaleInstantly(Vector2 scale)
    {
        _scaleCancelToken?.Cancel();
        Scale = scale;
    }

    public void SetPositionInstantly(Vector2 position)
    {
        _positionCancelToken?.Cancel();
        Position = position;
    }

    public void SetLibraryIndex(int displayIndex)
    {
        if (_libraryIndexLabel == null)
        {
            return;
        }

        _libraryIndexLabel.Text = displayIndex.ToString();
        _libraryIndexLabel.Visible = displayIndex > 0 && SaveManager.Instance.PrefsSave.ShowCardIndices;
    }

    public void UpdateCard(bool applyLibraryTint = true)
    {
        if (!IsNodeReady() || CardNode == null || !CardNode.IsNodeReady())
        {
            return;
        }

        BookLibraryUtility.ApplyLibraryCardPreviewVisuals(CardNode, applyLibraryTint);
        if (CombatManager.Instance is not { IsInProgress: true })
        {
            return;
        }

        if (CardNode.Model?.CanPlay() == true || ShouldGlowRed || ShouldGlowGold)
        {
            CardNode.CardHighlight.AnimShow();
            CardNode.CardHighlight.Modulate = NCardHighlight.playableColor;
            if (ShouldGlowRed)
            {
                CardNode.CardHighlight.Modulate = NCardHighlight.red;
            }
            else if (ShouldGlowGold)
            {
                CardNode.CardHighlight.Modulate = NCardHighlight.gold;
            }
        }
        else if (InSelectMode && IsSelected)
        {
            CardNode.CardHighlight.AnimShow();
            CardNode.CardHighlight.Modulate = NCardHighlight.playableColor;
        }
        else
        {
            CardNode.CardHighlight.AnimHide();
        }
    }

    public async Task PlayTransformAnim(CardModel endCard)
    {
        if (CardNode == null || !GodotObject.IsInstanceValid(CardNode) || endCard.Pile is null)
        {
            return;
        }

        if (TestMode.IsOn)
        {
            ApplyTransformVisual(endCard);
            UpdateCard();
            return;
        }

        var shineVfx = NCardTransformShineVfx.Create(CardNode, endCard, []);
        if (shineVfx is null)
        {
            ApplyTransformVisual(endCard);
            UpdateCard();
            return;
        }

        await shineVfx.PlayAnimation(shortVersion: true);
        UpdateCard();
    }

    private void ApplyTransformVisual(CardModel endCard)
    {
        if (CardNode == null || !GodotObject.IsInstanceValid(CardNode) || endCard.Pile is null)
        {
            return;
        }

        CardNode.Model = endCard;
        BookLibraryUtility.ApplyLibraryCardPreviewVisuals(CardNode);
    }

    public void Flash()
    {
        if (_flash == null || !GodotObject.IsInstanceValid(_flash))
        {
            return;
        }

        _flash.Scale = Vector2.One;
        _flash.Modulate = NCardHighlight.playableColor;
        if (ShouldGlowGold)
        {
            _flash.Modulate = NCardHighlight.gold;
        }
        else if (ShouldGlowRed)
        {
            _flash.Modulate = NCardHighlight.red;
        }

        _flashTween?.Kill();
        _flashTween = CreateTween();
        _flashTween.TweenProperty(_flash, "modulate:a", 0.6, 0.15);
        _flashTween.TweenProperty(_flash, "modulate:a", 0, 0.3);
    }

    private bool ShouldGlowGold
    {
        get
        {
            if (IsSelected)
            {
                return true;
            }

            if (CardNode?.Model is not { } cardModel || !cardModel.CanPlay() || !cardModel.ShouldGlowGold)
            {
                return false;
            }

            return cardModel.Owner?.PlayerCombatState?.Phase == PlayerTurnPhase.Play;
        }
    }

    private bool ShouldGlowRed =>
        CardNode?.Model is { ShouldGlowRed: true } cardModel
        && cardModel.Owner?.PlayerCombatState?.Phase == PlayerTurnPhase.Play;

    protected override void DoCardHoverEffects(bool isHovered)
    {
        if (isHovered && IsHandDraggingCard())
        {
            return;
        }

        ZIndex = isHovered ? 1 : 0;
        if (isHovered)
        {
            CreateHoverTips();
        }
        else
        {
            ClearHoverTips();
        }
    }

    protected override void OnMousePressed(InputEvent inputEvent)
    {
        if (!InSelectMode || IsHandDraggingCard())
        {
            return;
        }

        base.OnMousePressed(inputEvent);
    }

    protected override void OnMouseReleased(InputEvent inputEvent)
    {
        if (!InSelectMode || IsHandDraggingCard())
        {
            return;
        }

        base.OnMouseReleased(inputEvent);
    }

    private static bool IsHandDraggingCard() =>
        NCombatRoom.Instance?.Ui?.Hand?.InCardPlay ?? false;

    private void StopAnimations()
    {
        _angleCancelToken?.Cancel();
        _positionCancelToken?.Cancel();
        _scaleCancelToken?.Cancel();
    }

    private async Task AnimAngle(CancellationTokenSource cancelToken)
    {
        while (!cancelToken.IsCancellationRequested)
        {
            RotationDegrees = Mathf.Lerp(RotationDegrees, _targetAngle, (float)GetProcessDeltaTime() * RotateSpeed);
            if (Mathf.Abs(RotationDegrees - _targetAngle) < AngleSnapThreshold)
            {
                RotationDegrees = _targetAngle;
                break;
            }

            await this.AwaitProcessFrameNonThrowing(cancelToken);
        }
    }

    private async Task AnimScale(CancellationTokenSource cancelToken)
    {
        while (!cancelToken.IsCancellationRequested)
        {
            if (cancelToken.IsCancellationRequested)
            {
                return;
            }

            Scale = Scale.Lerp(_targetScale, (float)GetProcessDeltaTime() * ScaleSpeed);
            if (Mathf.Abs(_targetScale.X - Scale.X) < ScaleSnapThreshold)
            {
                Scale = _targetScale;
                return;
            }

            await this.AwaitProcessFrameNonThrowing(cancelToken);
        }
    }

    private async Task AnimPosition(CancellationTokenSource cancelToken)
    {
        while (!cancelToken.IsCancellationRequested)
        {
            Position = Position.Lerp(_targetPosition, (float)GetProcessDeltaTime() * MoveSpeed);
            if (Position.DistanceSquaredTo(_targetPosition) < PositionSnapThreshold)
            {
                Position = _targetPosition;
                return;
            }

            await this.AwaitProcessFrameNonThrowing(cancelToken);
        }
    }

    private void SubscribeToEvents(CardModel? card)
    {
        if (card == null || !IsInsideTree())
        {
            return;
        }

        card.Upgraded += OnCardVisualChanged;
        card.KeywordsChanged += OnCardVisualChanged;
        card.ReplayCountChanged += OnCardVisualChanged;
        card.AfflictionChanged += OnCardVisualChanged;
        card.EnergyCostChanged += OnCardVisualChanged;
        card.StarCostChanged += OnCardVisualChanged;
    }

    private void UnsubscribeFromEvents(CardModel? card)
    {
        if (card == null)
        {
            return;
        }

        card.Upgraded -= OnCardVisualChanged;
        card.KeywordsChanged -= OnCardVisualChanged;
        card.ReplayCountChanged -= OnCardVisualChanged;
        card.AfflictionChanged -= OnCardVisualChanged;
        card.EnergyCostChanged -= OnCardVisualChanged;
        card.StarCostChanged -= OnCardVisualChanged;
    }

    private void OnCardVisualChanged()
    {
        UpdateCard();
        Flash();
    }

    private void OnModelChanged(CardModel? oldModel)
    {
        UnsubscribeFromEvents(oldModel);
        SubscribeToEvents(CardNode?.Model);
        UpdateCard();
    }

    private void DetachCardNodeEvents()
    {
        if (CardNode != null)
        {
            CardNode.ModelChanged -= OnModelChanged;
        }
    }
}
