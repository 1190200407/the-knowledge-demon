using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ComicChess.KnowledgeDemon;

/// <summary>战斗 UI 上的藏书库/混合选牌遮罩与确认钮（节点挂在 <see cref="NCombatUi" /> 下，UI 子节点为同级兄弟以控制层级）。</summary>
public partial class NKnowledgeDemonCardSelectOverlay : Node
{
    private ColorRect? _backstop;
    private MegaRichTextLabel? _header;
    private NConfirmButton? _confirm;

    private KnowledgeDemonCardSelectSession? _session;

    public static NKnowledgeDemonCardSelectOverlay? Instance { get; private set; }

    public override void _EnterTree()
    {
        base._EnterTree();
        Instance = this;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        if (ReferenceEquals(Instance, this))
        {
            Instance = null;
        }
    }

    public static NKnowledgeDemonCardSelectOverlay EnsureReady()
    {
        if (Instance != null)
        {
            Instance.EnsureUiBuilt();
            return Instance;
        }

        var ui = NCombatRoom.Instance?.Ui;
        if (ui == null)
        {
            throw new InvalidOperationException("Combat UI is not ready for card selection.");
        }

        var overlay = new NKnowledgeDemonCardSelectOverlay();
        ui.AddChildSafely(overlay);
        overlay.EnsureUiBuilt();
        return overlay;
    }

    public override void _Ready() => EnsureUiBuilt();

    private void EnsureUiBuilt()
    {
        if (_backstop != null)
        {
            return;
        }

        var ui = GetParent<NCombatUi>();
        if (ui == null)
        {
            Entry.Logger.Error("[BookLibrary] Card select overlay parent is not NCombatUi");
            return;
        }

        _backstop = CreateBackstop();
        ui.AddChildSafely(_backstop);

        _header = CreateHeader();
        ui.AddChildSafely(_header);

        _confirm = SceneHelper.Instantiate<NConfirmButton>("ui/confirm_button");
        _confirm.Name = "KnowledgeDemonSelectConfirmButton";
        _confirm.Disable();
        _confirm.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(OnConfirmPressed));
        ui.AddChildSafely(_confirm);
    }

    public async Task<IEnumerable<CardModel>> RunSession(
        CardSelectorPrefs prefs,
        Func<CardModel, bool> filter,
        bool includeHand)
    {
        if (_session != null)
        {
            throw new InvalidOperationException("A knowledge demon card selection is already in progress.");
        }

        if (_backstop == null || _header == null || _confirm == null)
        {
            throw new InvalidOperationException("Card select overlay UI is not initialized.");
        }

        _session = new KnowledgeDemonCardSelectSession(this, prefs, filter, includeHand);
        try
        {
            return await _session.RunAsync();
        }
        finally
        {
            _session = null;
        }
    }

    internal bool TryHandleHandHolder(NCardHolder holder) =>
        _session?.TryHandleHandHolder(holder) ?? false;

    internal bool BlocksHandPlay =>
        _session is { IsActive: true, IncludeHand: true };

    internal void TryHandleLibraryHolder(NBookLibraryCardHolder holder) =>
        _session?.TryHandleLibraryHolder(holder);

    private void OnConfirmPressed(NButton _)
    {
        _session?.Confirm();
    }

    private static ColorRect CreateBackstop()
    {
        var backstop = new ColorRect
        {
            Name = "KnowledgeDemonSelectBackstop",
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Color = new Color(0f, 0f, 0f, 0.75f),
            AnchorRight = 1f,
            AnchorBottom = 1f,
            GrowHorizontal = Control.GrowDirection.Both,
            GrowVertical = Control.GrowDirection.Both,
        };
        backstop.SelfModulate = new Color(1f, 1f, 1f, 0f);
        return backstop;
    }

    private static MegaRichTextLabel CreateHeader()
    {
        var header = new MegaRichTextLabel
        {
            Name = "KnowledgeDemonSelectHeader",
            Visible = false,
            BbcodeEnabled = true,
            ScrollActive = false,
            AnchorTop = 0.5f,
            AnchorRight = 1f,
            AnchorBottom = 0.5f,
            OffsetTop = -342f,
            OffsetBottom = -292f,
            GrowHorizontal = Control.GrowDirection.Both,
            GrowVertical = Control.GrowDirection.Both,
        };
        return header;
    }

    internal ColorRect Backstop => _backstop!;
    internal MegaRichTextLabel Header => _header!;
    internal NConfirmButton ConfirmButton => _confirm!;
    internal NCombatUi CombatUi => GetParent<NCombatUi>()!;
}
