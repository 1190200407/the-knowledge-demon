using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace ComicChess.KnowledgeDemon;

/// <summary>变化选项排除会导致独一冲突的牌。</summary>
internal sealed class KnowledgeDemonUniqueTransformOptionsPatch : IPatchMethod
{
    public static string PatchId => "knowledgedemon_unique_transform_options";
    public static string Description => "Exclude unique cards that would violate the unique rule from transform options";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(CardFactory), "GetFilteredTransformationOptions", [
            typeof(CardModel),
            typeof(IEnumerable<CardModel>),
            typeof(bool),
        ]),
    ];

    public static void Postfix(CardModel original, ref CardModel[] __result)
    {
        if (__result is not { Length: > 0 } || original.Owner is not { } player)
        {
            return;
        }

        var filtered = __result
            .Where(candidate => original.IsInCombat
                ? !KnowledgeDemonUniqueUtility.WouldViolateCombatUniqueRule(player, candidate, original)
                : !KnowledgeDemonUniqueUtility.WouldViolateDeckUniqueRule(player, candidate, original))
            .ToArray();

        if (filtered.Length > 0)
        {
            __result = filtered;
        }
    }
}
