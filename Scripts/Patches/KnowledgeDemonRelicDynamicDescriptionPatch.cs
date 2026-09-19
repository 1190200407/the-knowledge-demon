using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching;
using STS2RitsuLib.Patching.Models;

namespace ComicChess.KnowledgeDemon;

internal sealed class KnowledgeDemonRelicDynamicDescriptionPatch : IPatchMethod
{
    public static string PatchId => "knowledge_demon_relic_dynamic_description_in_combat";
    public static string Description => "Provide the InCombat variable for Knowledge Demon relic descriptions";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Getter<RelicModel>(nameof(RelicModel.DynamicDescription)),
    ];

    public static void Postfix(RelicModel __instance, LocString __result)
    {
        if (__instance is KnowledgeDemonRelicModel)
        {
            __result.Add("InCombat", CombatManager.Instance.IsInProgress);
        }
    }
}
