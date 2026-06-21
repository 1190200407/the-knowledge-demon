using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using STS2RitsuLib.Patching.Models;

namespace ComicChess.KnowledgeDemon;

internal sealed class BookLibraryChooseScreenReadyPatch : IPatchMethod
{
    public static string PatchId => "knowledgedemon_book_library_choose_screen_ready";
    public static string Description => "Combat-aware card previews on library choose screen";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(NChooseACardSelectionScreen), nameof(NChooseACardSelectionScreen._Ready)),
    ];

    public static void Postfix(NChooseACardSelectionScreen __instance) =>
        KnowledgeDemonChooseContext.AttachChooseScreen(__instance);
}
