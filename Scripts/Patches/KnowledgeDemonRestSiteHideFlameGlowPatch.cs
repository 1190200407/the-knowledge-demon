using MegaCrit.Sts2.Core.Nodes.RestSite;
using STS2RitsuLib.Patching.Models;

namespace ComicChess.KnowledgeDemon;

internal sealed class KnowledgeDemonRestSiteHideFlameGlowPatch : IPatchMethod
{
    public static string PatchId => "knowledgedemon_rest_site_hide_flame_glow";
    public static string Description => "Knowledge Demon rest site: hide BodyFireLight when the campfire goes out";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter.HideFlameGlow)),
    ];

    public static void Prefix(NRestSiteCharacter __instance)
    {
        if (__instance is NKnowledgeDemonRestSiteCharacter character)
        {
            character.HideKnowledgeDemonFlameGlow();
        }
    }
}
