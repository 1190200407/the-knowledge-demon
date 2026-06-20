using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace ComicChess.KnowledgeDemon;

/// <summary>平行观测：变化选项包含其他角色色池的牌。</summary>
internal sealed class ParallelObservationTransformOptionsPatch : IPatchMethod
{
    public static string PatchId => "knowledgedemon_parallel_observation_transform_options";
    public static string Description => "Include other character card pools in transform options when Parallel Observation is active";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(CardFactory), "GetFilteredTransformationOptions", [
            typeof(CardModel),
            typeof(IEnumerable<CardModel>),
            typeof(bool),
        ]),
    ];

    public static void Postfix(CardModel original, bool isInCombat, ref CardModel[] __result)
    {
        if (__result is not { Length: > 0 }
            || original.Owner?.Creature.GetPower<ParallelObservationPower>() is null)
        {
            return;
        }

        var existingIds = __result.Select(card => card.Id).ToHashSet();
        var additional = TransformOptionUtility
            .FilterTransformCandidates(
                original,
                TransformOptionUtility.GetOtherCharacterPoolCards(original.Owner, original.Pool),
                isInCombat)
            .Where(card => !existingIds.Contains(card.Id))
            .ToArray();

        if (additional.Length == 0)
        {
            return;
        }

        __result = __result.Concat(additional).ToArray();
    }
}
