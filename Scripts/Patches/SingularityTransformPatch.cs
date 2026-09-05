using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.Patching.Models;

namespace ComicChess.KnowledgeDemon;

internal sealed class SingularityTransformPatch : IPatchMethod
{
    public static string PatchId => "knowledgedemon_singularity_transform";
    public static string Description => "Force combat transformations to use the card selected by Singularity";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(CardCmd), nameof(CardCmd.Transform), [
            typeof(IEnumerable<CardTransformation>),
            typeof(Rng),
            typeof(CardPreviewStyle),
        ]),
    ];

    public static void Prefix(ref IEnumerable<CardTransformation> transformations)
    {
        transformations = OverrideTransformations(transformations);
    }

    private static IEnumerable<CardTransformation> OverrideTransformations(
        IEnumerable<CardTransformation> transformations)
    {
        foreach (var transformation in transformations)
        {
            var original = transformation.Original;
            var power = original.Owner?.Creature.GetPower<SingularityPower>();
            if (power?.CreateReplacement(original) is { } result)
            {
                yield return new CardTransformation(original, result.Replacement);
                continue;
            }

            yield return transformation;
        }
    }
}
