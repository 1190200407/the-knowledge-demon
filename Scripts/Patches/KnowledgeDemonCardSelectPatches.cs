using Godot;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using STS2RitsuLib.Patching.Models;

namespace ComicChess.KnowledgeDemon;

internal sealed class KnowledgeDemonCardSelectOverlayInjectPatch : IPatchMethod
{
    public static string PatchId => "knowledgedemon_card_select_overlay_inject";
    public static string Description => "Inject NKnowledgeDemonCardSelectOverlay into NCombatUi";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(NCombatUi), nameof(NCombatUi._Ready)),
    ];

    public static void Postfix(NCombatUi __instance)
    {
        if (NKnowledgeDemonCardSelectOverlay.Instance != null)
        {
            return;
        }

        var overlay = new NKnowledgeDemonCardSelectOverlay();
        __instance.AddChildSafely(overlay);
    }
}

internal sealed class KnowledgeDemonHandCardSelectPatch : IPatchMethod
{
    public static string PatchId => "knowledgedemon_hand_card_select";
    public static string Description => "Route hand holder clicks to knowledge demon mixed card selection";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(NPlayerHand), "OnHolderPressed", [typeof(NCardHolder)]),
    ];

    public static bool Prefix(NPlayerHand __instance, NCardHolder holder)
    {
        if (NKnowledgeDemonCardSelectOverlay.Instance?.TryHandleHandHolder(holder) == true)
        {
            return false;
        }

        return true;
    }
}

internal sealed class KnowledgeDemonDisableHandPlayDuringSelectPatch : IPatchMethod
{
    public static string PatchId => "knowledgedemon_disable_hand_play_during_mixed_select";
    public static string Description => "Block hand card play while mixed library/hand selection is active";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(NPlayerHand), "CanPlayCards"),
    ];

    public static bool Prefix(ref bool __result)
    {
        if (NKnowledgeDemonCardSelectOverlay.Instance?.BlocksHandPlay == true)
        {
            __result = false;
            return false;
        }

        return true;
    }
}
