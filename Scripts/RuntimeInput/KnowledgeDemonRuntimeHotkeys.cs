using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.RuntimeInput;

namespace ComicChess.KnowledgeDemon;

public static class KnowledgeDemonRuntimeHotkeys
{
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
            "C",
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
