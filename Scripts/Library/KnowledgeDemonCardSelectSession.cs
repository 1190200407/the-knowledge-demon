using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ComicChess.KnowledgeDemon;

internal sealed class KnowledgeDemonCardSelectSession
{
    private readonly NKnowledgeDemonCardSelectOverlay _overlay;
    private readonly CardSelectorPrefs _prefs;
    private readonly Func<CardModel, bool> _filter;
    private readonly bool _includeHand;
    private readonly List<CardModel> _selected = [];
    private readonly List<NBookLibraryCardHolder> _libraryHolders = [];
    private readonly List<NHandCardHolder> _handHolders = [];
    private readonly Dictionary<CardModel, int> _libraryDisplayIndices = new();

    private readonly Callable _libraryHolderPressedCallable;

    private TaskCompletionSource<IEnumerable<CardModel>>? _completionSource;
    private Tween? _backstopTween;
    private int _savedBackstopIndex;
    private int _savedHeaderIndex;
    private int _savedConfirmIndex;
    private int _savedLibraryIndex = -1;
    private int _savedHandIndex = -1;
    private bool _handCardsHidden;

    internal bool IncludeHand => _includeHand;

    internal bool IsActive => _completionSource != null;

    public KnowledgeDemonCardSelectSession(
        NKnowledgeDemonCardSelectOverlay overlay,
        CardSelectorPrefs prefs,
        Func<CardModel, bool> filter,
        bool includeHand)
    {
        _overlay = overlay;
        _prefs = prefs;
        _filter = filter;
        _includeHand = includeHand;
        _libraryHolderPressedCallable = Callable.From<NCardHolder>(OnLibraryHolderPressed);
    }

    public async Task<IEnumerable<CardModel>> RunAsync()
    {
        _completionSource = new TaskCompletionSource<IEnumerable<CardModel>>();
        Enter();
        var result = await _completionSource.Task;
        Exit();
        return result;
    }

    public bool TryHandleHandHolder(NCardHolder holder)
    {
        if (!_includeHand)
        {
            return false;
        }

        var hand = NCombatRoom.Instance?.Ui?.Hand;
        if (hand == null || hand.IsInCardSelection)
        {
            return false;
        }

        if (holder is not NHandCardHolder handHolder || handHolder.CardNode?.Model is not CardModel card)
        {
            return false;
        }

        if (!_filter(card))
        {
            return false;
        }

        Toggle(card);
        return true;
    }

    public void TryHandleLibraryHolder(NBookLibraryCardHolder holder)
    {
        if (holder.CardNode?.Model is not CardModel card || !_filter(card))
        {
            return;
        }

        Toggle(card);
    }

    public void Confirm()
    {
        if (_completionSource == null)
        {
            return;
        }

        var count = _selected.Count;
        if (count < _prefs.MinSelect || count > _prefs.MaxSelect)
        {
            return;
        }

        _completionSource.TrySetResult(_selected.ToList());
    }

    private void Enter()
    {
        var ui = _overlay.CombatUi;
        var backstop = _overlay.Backstop;
        var header = _overlay.Header;
        var confirm = _overlay.ConfirmButton;

        _savedBackstopIndex = backstop.GetIndex();
        _savedHeaderIndex = header.GetIndex();
        _savedConfirmIndex = confirm.GetIndex();

        var library = NBookLibraryPile.Instance;
        var hand = ui.Hand;

        if (library is not null)
        {
            _savedLibraryIndex = library.GetIndex();
        }

        if (_includeHand)
        {
            _savedHandIndex = hand.GetIndex();
        }

        var backstopIndex = ui.PlayContainer.GetIndex() + 1;
        ui.MoveChildSafely(backstop, backstopIndex);

        backstop.Visible = true;
        backstop.MouseFilter = Control.MouseFilterEnum.Stop;
        backstop.SelfModulate = new Color(1f, 1f, 1f, 0f);
        _backstopTween = backstop.CreateTween();
        _backstopTween.TweenProperty(backstop, "self_modulate:a", 1f, 0.2f);

        var layerIndex = backstop.GetIndex() + 1;
        if (library is not null)
        {
            ui.MoveChildSafely(library, layerIndex);
            layerIndex = library.GetIndex() + 1;
        }

        if (_includeHand)
        {
            ui.MoveChildSafely(hand, layerIndex);
        }

        if (!_includeHand)
        {
            hand.CardHolderContainer.Visible = false;
            _handCardsHidden = true;
        }

        header.Visible = true;
        header.Text = "[center]" + _prefs.Prompt.GetFormattedText() + "[/center]";
        confirm.Visible = true;
        header.MoveToFrontSafely();
        confirm.MoveToFrontSafely();

        NCombatRoom.Instance?.RestrictControllerNavigation([]);
        CollectLibraryHolders();
        if (_includeHand)
        {
            CollectHandHolders(hand);
        }

        RefreshHolderVisuals();
        RefreshConfirmButton();
    }

    private void Exit()
    {
        _backstopTween?.Kill();

        var ui = _overlay.CombatUi;
        var backstop = _overlay.Backstop;
        var header = _overlay.Header;
        var confirm = _overlay.ConfirmButton;

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

            if (holder.CardNode?.Model is CardModel card
                && _libraryDisplayIndices.TryGetValue(card, out var pileIndex))
            {
                holder.SetLibraryIndex(pileIndex);
            }

            ClearSelectionHighlight(holder.CardNode);
            holder.UpdateCard();
        }

        foreach (var holder in _handHolders)
        {
            if (!GodotObject.IsInstanceValid(holder))
            {
                continue;
            }

            holder.InSelectMode = false;
            holder.SetIndexLabel(0);
            if (holder.CardNode is not null)
            {
                holder.CardNode.SetPretendCardCanBePlayed(false);
                holder.CardNode.SetForceUnpoweredPreview(false);
                ClearSelectionHighlight(holder.CardNode);
            }

            holder.Visible = true;
            holder.UpdateCard();
        }

        _libraryHolders.Clear();
        _handHolders.Clear();
        _selected.Clear();
        _libraryDisplayIndices.Clear();

        backstop.Visible = false;
        backstop.MouseFilter = Control.MouseFilterEnum.Ignore;
        var fadeTween = backstop.CreateTween();
        fadeTween.TweenProperty(backstop, "self_modulate:a", 0f, 0.2f);

        header.Visible = false;
        confirm.Disable();
        confirm.Visible = false;

        ui.MoveChildSafely(backstop, _savedBackstopIndex);
        ui.MoveChildSafely(header, _savedHeaderIndex);
        ui.MoveChildSafely(confirm, _savedConfirmIndex);

        if (_savedHandIndex >= 0)
        {
            ui.MoveChildSafely(ui.Hand, _savedHandIndex);
            _savedHandIndex = -1;
        }

        if (_savedLibraryIndex >= 0 && NBookLibraryPile.Instance is { } library)
        {
            ui.MoveChildSafely(library, _savedLibraryIndex);
            _savedLibraryIndex = -1;
        }

        if (_handCardsHidden)
        {
            ui.Hand.CardHolderContainer.Visible = true;
            _handCardsHidden = false;
        }

        _completionSource = null;
        NCombatRoom.Instance?.EnableControllerNavigation();
    }

    private void CollectLibraryHolders()
    {
        var pile = NBookLibraryPile.Instance;
        if (pile == null)
        {
            return;
        }

        foreach (var holder in pile.GetHolderSnapshot())
        {
            if (!GodotObject.IsInstanceValid(holder))
            {
                continue;
            }

            if (holder.CardNode?.Model is CardModel card)
            {
                _libraryDisplayIndices[card] = _libraryDisplayIndices.Count + 1;
            }

            holder.Connect(NCardHolder.SignalName.Pressed, _libraryHolderPressedCallable);
            _libraryHolders.Add(holder);
            holder.InSelectMode = true;
        }
    }

    private void OnLibraryHolderPressed(NCardHolder holder)
    {
        if (holder is NBookLibraryCardHolder libraryHolder)
        {
            TryHandleLibraryHolder(libraryHolder);
        }
    }

    private void CollectHandHolders(NPlayerHand hand)
    {
        foreach (var holder in hand.ActiveHolders)
        {
            _handHolders.Add(holder);
            holder.InSelectMode = true;
            if (holder.CardNode is not null)
            {
                holder.CardNode.SetPretendCardCanBePlayed(false);
                holder.CardNode.SetForceUnpoweredPreview(_prefs.UnpoweredPreviews);
            }
        }
    }

    private void Toggle(CardModel card)
    {
        if (_selected.Contains(card))
        {
            _selected.Remove(card);
        }
        else if (_selected.Count >= _prefs.MaxSelect)
        {
            if (_prefs.MaxSelect <= 1)
            {
                _selected.Clear();
                _selected.Add(card);
            }
            else
            {
                _selected.RemoveAt(_selected.Count - 1);
                _selected.Add(card);
            }
        }
        else
        {
            _selected.Add(card);
        }

        RefreshHolderVisuals();
        RefreshConfirmButton();

        if (!_prefs.RequireManualConfirmation && _selected.Count >= _prefs.MaxSelect)
        {
            Confirm();
        }
    }

    private void RefreshHolderVisuals()
    {
        foreach (var holder in _libraryHolders)
        {
            if (!GodotObject.IsInstanceValid(holder) || holder.CardNode?.Model is not CardModel card)
            {
                continue;
            }

            var selectionIndex = _selected.IndexOf(card);
            holder.IsSelected = selectionIndex >= 0;
            holder.Visible = _filter(card);
            holder.UpdateCard();

            if (selectionIndex >= 0)
            {
                holder.SetLibraryIndex(selectionIndex + 1);
                SetSelectionHighlight(holder.CardNode, true);
            }
            else
            {
                if (_libraryDisplayIndices.TryGetValue(card, out var pileIndex))
                {
                    holder.SetLibraryIndex(pileIndex);
                }

                ClearSelectionHighlight(holder.CardNode);
            }
        }

        foreach (var holder in _handHolders)
        {
            if (!GodotObject.IsInstanceValid(holder) || holder.CardNode?.Model is not CardModel card)
            {
                continue;
            }

            var selectionIndex = _selected.IndexOf(card);
            var isSelected = selectionIndex >= 0;
            holder.Visible = _filter(card);
            holder.CardNode.SetPretendCardCanBePlayed(isSelected);
            holder.CardNode.SetForceUnpoweredPreview(_prefs.UnpoweredPreviews);
            holder.SetIndexLabel(isSelected ? selectionIndex + 1 : 0);
            holder.UpdateCard();
            SetSelectionHighlight(holder.CardNode, isSelected);
        }
    }

    private static void SetSelectionHighlight(NCard? cardNode, bool selected)
    {
        if (cardNode is null || !GodotObject.IsInstanceValid(cardNode))
        {
            return;
        }

        if (selected)
        {
            cardNode.CardHighlight.AnimShow();
            cardNode.CardHighlight.Modulate = NCardHighlight.playableColor;
        }
        else
        {
            ClearSelectionHighlight(cardNode);
        }
    }

    private static void ClearSelectionHighlight(NCard? cardNode)
    {
        if (cardNode is null || !GodotObject.IsInstanceValid(cardNode))
        {
            return;
        }

        cardNode.CardHighlight.AnimHide();
    }

    private void RefreshConfirmButton()
    {
        var count = _selected.Count;
        if (count >= _prefs.MinSelect && count <= _prefs.MaxSelect)
        {
            _overlay.ConfirmButton.Enable();
        }
        else
        {
            _overlay.ConfirmButton.Disable();
        }
    }
}
