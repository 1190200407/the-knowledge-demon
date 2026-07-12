using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.RuntimeInput;

namespace ComicChess.KnowledgeDemon;

public static class KnowledgeDemonRuntimeHotkeys
{
    public const string DefaultToggleLibraryBinding = "C";

    private static readonly LocString LibraryTitle =
        new("static_hover_tips", "KNOWLEDGE_DEMON_CARDPILE_LIBRARY.title");

    private static readonly LocString LibraryHotkeyDescription =
        new("static_hover_tips", "KNOWLEDGE_DEMON_CARDPILE_LIBRARY.hotkey");

    private static IRuntimeHotkeyHandle? _toggleLibraryHandle;

    public static void Register()
    {
        if (_toggleLibraryHandle is { IsRegistered: true })
        {
            return;
        }

        _toggleLibraryHandle = RuntimeHotkeyService.Register(
            KnowledgeDemonModSettingsPage.GetToggleLibraryHotkey(),
            ToggleLibraryVisibility,
            new RuntimeHotkeyOptions
            {
                Id = "knowledge_demon.toggle_library_pile",
                DisplayName = RuntimeHotkeyText.Dynamic(() => LibraryTitle.GetFormattedText()),
                Description = RuntimeHotkeyText.Dynamic(() => LibraryHotkeyDescription.GetFormattedText()),
                Category = "Knowledge Demon",
                Purpose = "toggle_visibility",
                MarkInputHandled = true,
                DebugName = "Knowledge Demon library visibility",
            });
    }

    public static bool TryRebindToggleLibraryHotkey(string binding)
    {
        var handle = _toggleLibraryHandle;
        if (handle is not { IsRegistered: true })
        {
            return false;
        }

        return handle.TryRebind(binding, out _);
    }

    private static void ToggleLibraryVisibility()
    {
        var pile = NBookLibraryPile.Instance;
        var player = pile?.Player;
        if (pile == null || player == null || !LocalContext.IsMe(player))
        {
            return;
        }

        if (!BookLibraryUtility.PlayerHasBookLibraryRelic(player))
        {
            return;
        }

        pile.ToggleCardsVisible();
    }
}
