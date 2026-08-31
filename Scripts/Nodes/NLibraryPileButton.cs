using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace ComicChess.KnowledgeDemon;

public partial class NLibraryPileButton : NButton
{
    public const string NodeAttachmentLocalId = "library_pile_button";
    public const string NodeAttachmentName = "LibraryPileButton";
    public const string ScenePath = "res://KnowledgeDemon/scenes/library_pile_button.tscn";
    public static readonly Vector2 DefaultPosition = new(110f, 985f);

    private static readonly Vector2 HoverScale = Vector2.One * 1.25f;
    private static readonly Color PressedColor = Colors.DarkGray;
    private static readonly Vector2 VisibilityHideOffset = new(-150f, 100f);

    private Control? _icon;
    private MegaLabel? _countLabel;
    private Tween? _bumpTween;
    private Tween? _visibilityTween;
    private CardPile? _pile;
    private Player? _player;
    private Vector2 _showPosition = DefaultPosition;
    private Vector2 _hidePosition = DefaultPosition + VisibilityHideOffset;
    private bool _isAvailabilityVisible;

    public static NLibraryPileButton? Instance { get; private set; }

    public Player? Player => _player;

    public void ApplyConfiguredPosition()
    {
        _showPosition = KnowledgeDemonModSettingsPage.GetLibraryUiPosition();
        _hidePosition = _showPosition + VisibilityHideOffset;
        Position = _isAvailabilityVisible ? _showPosition : _hidePosition;
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        Instance = this;
    }

    public override void _Ready()
    {
        FocusMode = FocusModeEnum.All;
        ConnectSignals();
        Visible = false;
    }

    protected override void ConnectSignals()
    {
        base.ConnectSignals();
        _icon = GetNode<Control>("Icon");
        _countLabel = GetNode<MegaLabel>("CountContainer/Count");
    }

    protected override string[] Hotkeys => new string[1] { MegaInput.select };

    public override void _ExitTree()
    {
        base._ExitTree();
        if (ReferenceEquals(Instance, this))
        {
            Instance = null;
        }

        DetachPile();
    }

    public void Initialize(Player player)
    {
        _player = player;
        AttachPile(BookLibraryUtility.TryGetLibraryPile(player));
        RefreshCount();
        RefreshAvailability(immediate: true);
        RefreshToggleVisual();
    }

    public void RefreshAvailability(bool immediate = false)
    {
        var shouldBeVisible = _player != null && BookLibraryUtility.PlayerHasBookLibraryAccess(_player);
        if (shouldBeVisible == _isAvailabilityVisible && !immediate)
        {
            return;
        }

        _isAvailabilityVisible = shouldBeVisible;
        if (shouldBeVisible)
        {
            ShowButton(immediate);
        }
        else
        {
            HideButton(immediate);
        }
    }

    public void AnimIn()
    {
        if (!_isAvailabilityVisible)
        {
            return;
        }

        _visibilityTween?.Kill();
        Position = _hidePosition;
        _visibilityTween = CreateTween();
        _visibilityTween.TweenProperty(this, "position", _showPosition, 0.5f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Expo);
    }

    public void AnimOut()
    {
        if (!_isAvailabilityVisible)
        {
            return;
        }

        _visibilityTween?.Kill();
        Position = _showPosition;
        _visibilityTween = CreateTween();
        _visibilityTween.TweenProperty(this, "position", _hidePosition, 0.5f)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Back);
    }

    public void RefreshToggleVisual()
    {
        if (_icon == null)
        {
            return;
        }

        var isShown = NBookLibraryPile.Instance?.AreCardsShownByToggle ?? true;
        _icon.Modulate = isShown ? Colors.White : new Color(0.72f, 0.72f, 0.72f, 1f);
    }

    protected override void OnPress()
    {
        base.OnPress();
        if (_icon == null)
        {
            return;
        }

        _bumpTween?.Kill();
        _bumpTween = CreateTween().SetParallel();
        _bumpTween.TweenProperty(_icon, "scale", Vector2.One, 0.25)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        _bumpTween.TweenProperty(_icon, "modulate", PressedColor, 0.25)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
    }

    protected override void OnRelease()
    {
        base.OnRelease();
        NBookLibraryPile.Instance?.ToggleCardsVisible();
        PlayReleaseAnim();
    }

    protected override void OnFocus()
    {
        base.OnFocus();
        var hoverTip = new HoverTip(
            new LocString("static_hover_tips", "KNOWLEDGE_DEMON_CARDPILE_LIBRARY.title"),
            new LocString("static_hover_tips", "KNOWLEDGE_DEMON_CARDPILE_LIBRARY.description"));
        var tipSet = NHoverTipSet.CreateAndShow(this, hoverTip);
        if (tipSet != null)
        {
            tipSet.GlobalPosition = GlobalPosition + new Vector2(-56f, -375f);
            tipSet.SetAlignment(this, HoverTipAlignment.None);
            KeepHoverTipWithinViewport(tipSet);
        }

        PlayHoverAnim();
    }

    protected override void OnUnfocus()
    {
        NHoverTipSet.Remove(this);
        PlayUnhoverAnim();
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
            RefreshCount();
            return;
        }

        _pile.CardAdded += OnPileChanged;
        _pile.CardRemoved += OnPileChanged;
        RefreshCount();
    }

    private void DetachPile()
    {
        if (_pile == null)
        {
            return;
        }

        _pile.CardAdded -= OnPileChanged;
        _pile.CardRemoved -= OnPileChanged;
        _pile = null;
    }

    private void OnPileChanged(CardModel _)
    {
        RefreshCount();
        PlayCountBump();
    }

    private void RefreshCount()
    {
        if (_countLabel == null)
        {
            return;
        }

        var count = _pile?.Cards.Count ?? 0;
        _countLabel.Text = count.ToString();
        _countLabel.PivotOffset = _countLabel.Size * 0.5f;
    }

    private void PlayHoverAnim()
    {
        if (_icon == null)
        {
            return;
        }

        _bumpTween?.Kill();
        _bumpTween = CreateTween();
        _bumpTween.TweenProperty(_icon, "scale", HoverScale, 0.05);
    }

    private void PlayUnhoverAnim()
    {
        if (_icon == null)
        {
            return;
        }

        _bumpTween?.Kill();
        _bumpTween = CreateTween().SetParallel();
        _bumpTween.TweenProperty(_icon, "scale", Vector2.One, 0.5)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Expo);
        _bumpTween.TweenProperty(_icon, "modulate", Colors.White, 0.5)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Expo);
    }

    private void PlayReleaseAnim()
    {
        if (_icon == null)
        {
            return;
        }

        _bumpTween?.Kill();
        _bumpTween = CreateTween();
        _bumpTween.TweenProperty(_icon, "scale", IsFocused ? HoverScale : Vector2.One, 0.05);
        _bumpTween.TweenProperty(_icon, "modulate", Colors.White, 0.5)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Expo);
    }

    private void PlayCountBump()
    {
        if (_icon == null || _countLabel == null)
        {
            return;
        }

        _bumpTween?.Kill();
        _bumpTween = CreateTween().SetParallel();
        _icon.Scale = HoverScale;
        _bumpTween.TweenProperty(_icon, "scale", Vector2.One, 0.5)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Expo);
        _countLabel.Scale = HoverScale;
        _bumpTween.TweenProperty(_countLabel, "scale", Vector2.One, 0.5)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Expo);
    }

    private void ShowButton(bool immediate)
    {
        _visibilityTween?.Kill();
        Visible = true;

        if (immediate)
        {
            Position = _showPosition;
            return;
        }

        Position = _hidePosition;
        _visibilityTween = CreateTween();
        _visibilityTween.TweenProperty(this, "position", _showPosition, 0.5f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Expo);
    }

    private void HideButton(bool immediate)
    {
        _visibilityTween?.Kill();
        if (immediate)
        {
            Visible = false;
            Position = _hidePosition;
            return;
        }

        _visibilityTween = CreateTween();
        _visibilityTween.TweenProperty(this, "position", _hidePosition, 0.5f)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Back);
        _visibilityTween.Chain().TweenCallback(Callable.From(() =>
        {
            Visible = false;
        }));
    }

    private void KeepHoverTipWithinViewport(NHoverTipSet tipSet)
    {
        var textContainer = tipSet.GetNodeOrNull<Control>("textHoverTipContainer");
        if (textContainer == null)
        {
            return;
        }

        var viewportSize = GetViewport().GetVisibleRect().Size;
        var maxPosition = new Vector2(
            Mathf.Max(0f, viewportSize.X - textContainer.Size.X),
            Mathf.Max(0f, viewportSize.Y - textContainer.Size.Y));
        textContainer.GlobalPosition = new Vector2(
            Mathf.Clamp(textContainer.GlobalPosition.X, 0f, maxPosition.X),
            Mathf.Clamp(textContainer.GlobalPosition.Y, 0f, maxPosition.Y));
    }
}
