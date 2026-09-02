using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Ftue;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;

namespace ComicChess.KnowledgeDemon;

public partial class NKnowledgeDemonRuleFtue : NFtue
{
    public const string id = "knowledge_demon_rule_ftue";
    public const string UniqueId = "unique_rule_ftue";
    public const string InfinityId = "infinity_rule_ftue";

    public enum RuleType
    {
        KnowledgeDemon,
        Unique,
        Infinity,
    }

    private static readonly Vector2 _pageAnimOffset = new(200f, 0f);

    [Export]
    private string _tutorialId = id;

    [Export]
    private string _titleKey = "KNOWLEDGE_DEMON_RULE_FTUE_TITLE";

    [Export]
    private string _bodyKeyPrefix = "KNOWLEDGE_DEMON_RULE_FTUE_BODY_";

    [Export]
    private int _totalPages = 3;

    [Export(PropertyHint.None, "")]
    private Texture2D _image1 = null!;

    [Export(PropertyHint.None, "")]
    private Texture2D? _image2;

    [Export(PropertyHint.None, "")]
    private Texture2D? _image3;

    private NButton _prevButton = null!;
    private NButton _nextButton = null!;
    private MegaLabel _pageCount = null!;
    private TextureRect _image = null!;
    private MegaRichTextLabel _bodyText = null!;
    private MegaLabel _header = null!;
    private Tween? _pageTurnTween;
    private TaskCompletionSource<bool>? _completionSource;
    private Vector2 _imagePosition;
    private Vector2 _textPosition;
    private int _currentPage = 1;

    public override void _Ready()
    {
        _image = GetNode<TextureRect>("Image");
        _bodyText = GetNode<MegaRichTextLabel>("Description");
        _pageCount = GetNode<MegaLabel>("PageCount");
        _header = GetNode<MegaLabel>("Header");
        _prevButton = GetNode<NButton>("LeftArrow");
        _nextButton = GetNode<NButton>("RightArrow");

        _prevButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(ToggleLeft));
        _nextButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(ToggleRight));

        _image.Modulate = Colors.Transparent;
        _bodyText.Modulate = Colors.Transparent;
        _prevButton.Visible = false;
        _prevButton.Disable();
        _nextButton.Visible = false;
        _nextButton.Disable();
        _pageCount.Visible = false;
        _header.Visible = false;
    }

    public static NKnowledgeDemonRuleFtue? Create() => Create(RuleType.KnowledgeDemon);

    public void Start()
    {
        NModalContainer.Instance?.ShowBackstop();
        _completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        _currentPage = 1;
        _imagePosition = _image.Position;
        _textPosition = _bodyText.Position;

        _header.SetTextAutoSize(new LocString("ftues", _titleKey).GetFormattedText());
        SetPageText(1);

        _nextButton.Visible = true;
        _nextButton.Enable();
        _pageCount.Visible = true;
        _header.Visible = true;
        PlayPageAnim(forward: true);
    }

    public async Task WaitForPlayerToConfirm()
    {
        await _nextButton.AwaitSignal<NClickableControl>(NClickableControl.SignalName.Released, this);
    }

    private void ToggleLeft(NButton _)
    {
        if (_currentPage <= 1)
        {
            return;
        }

        _currentPage--;
        if (_currentPage == 1)
        {
            _prevButton.Visible = false;
            _prevButton.Disable();
        }

        SetPageText(_currentPage);
        PlayPageAnim(forward: false);
    }

    private void ToggleRight(NButton _)
    {
        if (_currentPage >= _totalPages)
        {
            SaveManager.Instance.MarkFtueAsComplete(_tutorialId);
            CloseFtue();
            return;
        }

        _currentPage++;
        _prevButton.Visible = true;
        _prevButton.Enable();

        SetPageText(_currentPage);
        PlayPageAnim(forward: true);
    }

    private void SetPageText(int page)
    {
        page = Math.Clamp(page, 1, _totalPages);
        _bodyText.SetTextAutoSize(GetBodyLocString(page).GetFormattedText());
        _image.Texture = page switch
        {
            1 => _image1,
            2 => _image2,
            _ => _image3,
        };

        var locString = new LocString("ftues", "COMBAT_BASICS_FTUE_PAGE_COUNT");
        locString.Add("totalPages", (decimal)_totalPages);
        locString.Add("currentPage", (decimal)page);
        _pageCount.SetTextAutoSize(locString.GetFormattedText());
    }

    private LocString GetBodyLocString(int page)
    {
        for (var candidatePage = page; candidatePage >= 1; candidatePage--)
        {
            var locString = LocString.GetIfExists("ftues", $"{_bodyKeyPrefix}{candidatePage}");
            if (locString is not null)
            {
                if (candidatePage != page)
                {
                    Entry.Logger.Warn($"[KnowledgeDemon][FTUE] Missing body key {_bodyKeyPrefix}{page}, falling back to {_bodyKeyPrefix}{candidatePage}.");
                }

                return locString;
            }
        }

        Entry.Logger.Warn($"[KnowledgeDemon][FTUE] Missing all body keys for prefix {_bodyKeyPrefix}.");
        return new LocString("ftues", "KNOWLEDGE_DEMON_RULE_FTUE_BODY_1");
    }

    public static NKnowledgeDemonRuleFtue? Create(RuleType ruleType)
    {
        if (TestMode.IsOn)
        {
            return null;
        }

        var scenePath = ruleType switch
        {
            RuleType.Unique => "res://KnowledgeDemon/scenes/ftue/unique_rules_ftue.tscn",
            RuleType.Infinity => "res://KnowledgeDemon/scenes/ftue/infinity_rules_ftue.tscn",
            _ => "res://KnowledgeDemon/scenes/ftue/knowledge_demon_rules_ftue.tscn",
        };

        return PreloadManager.Cache
            .GetScene(scenePath)
            .Instantiate<NKnowledgeDemonRuleFtue>(PackedScene.GenEditState.Disabled);
    }

    private void PlayPageAnim(bool forward)
    {
        _pageTurnTween?.Kill();
        _pageTurnTween = CreateTween().SetParallel();
        _pageTurnTween.TweenProperty(_image, "modulate:a", 1f, 0.5).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic).From(0.5f);
        _pageTurnTween.TweenProperty(_image, "position", _imagePosition, 0.5).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo)
            .From(forward ? _imagePosition + _pageAnimOffset : _imagePosition - _pageAnimOffset);
        _pageTurnTween.TweenProperty(_bodyText, "position", _textPosition, 0.5).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo)
            .From(forward ? _textPosition + _pageAnimOffset : _textPosition - _pageAnimOffset);
        _pageTurnTween.TweenProperty(_bodyText, "modulate:a", 1f, 0.6).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Linear).From(0f);
        _pageTurnTween.TweenProperty(_bodyText, "visible_ratio", 1f, 0.6).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Sine).From(0f);
    }

    public Task WaitForCompletionAsync()
    {
        return _completionSource?.Task ?? Task.CompletedTask;
    }

    public override void _ExitTree()
    {
        _completionSource?.TrySetResult(true);
        base._ExitTree();
    }
}
