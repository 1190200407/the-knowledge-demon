using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.Relics;
using STS2RitsuLib.Patching;
using STS2RitsuLib.Patching.Models;

namespace ComicChess.KnowledgeDemon;

internal sealed class TouchOfOrobasStarterRelicHoverTipPatch : IPatchMethod
{
    private static readonly AccessTools.FieldRef<TouchOfOrobas, List<IHoverTip>> ExtraHoverTipsRef =
        PrivateAccess.FieldRef<TouchOfOrobas, List<IHoverTip>>("_extraHoverTips");

    public static string PatchId => "knowledgedemon_touch_of_orobas_starter_relic_filter_normal_keyword_tips";
    public static string Description => "Remove non-omega choose/knowledge overload hover tips from Touch of Orobas starter relic preview";
    public static bool IsCritical => false;

    private const string chooseId = "LocString with Title=card_keywords.KNOWLEDGE_DEMON_KEYWORD_CHOOSE.title and Description=card_keywords.KNOWLEDGE_DEMON_KEYWORD_CHOOSE.description";
    private const string knowledgeOverloadId = "LocString with Title=card_keywords.KNOWLEDGE_DEMON_KEYWORD_KNOWLEDGE_OVERLOAD.title and Description=card_keywords.KNOWLEDGE_DEMON_KEYWORD_KNOWLEDGE_OVERLOAD.description";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Setter<TouchOfOrobas>(nameof(TouchOfOrobas.StarterRelic)),
    ];

    public static void Postfix(TouchOfOrobas __instance)
    {
        var extraHoverTips = ExtraHoverTipsRef(__instance);
        if (extraHoverTips.Count == 0)
        {
            return;
        }

        extraHoverTips.RemoveAll(static tip =>
            tip.Id == chooseId
            || tip.Id == knowledgeOverloadId);
    }
}
