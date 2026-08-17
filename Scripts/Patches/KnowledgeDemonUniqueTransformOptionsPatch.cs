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
        if (original.Owner is not { } player)
        {
            return;
        }

        var candidates = (__result ?? []).AsEnumerable();
        var environmentalToleranceActive =
            original.Owner.Creature.GetPower<EnvironmentalTolerancePower>() is not null;
        if (original.Type == CardType.Status && environmentalToleranceActive)
        {
            candidates = TransformOptionUtility.GetEnvironmentalToleranceTransformCandidates(
                player,
                original,
                original.IsInCombat);
        }

        var filtered = candidates
            .Where(candidate => original.IsInCombat
                ? !KnowledgeDemonUniqueUtility.WouldViolateCombatUniqueRule(player, candidate, original)
                : !KnowledgeDemonUniqueUtility.WouldViolateDeckUniqueRule(player, candidate, original))
            .GroupBy(card => card.Id)
            .Select(group => group.First())
            .ToArray();

        if (filtered.Length > 0)
        {
            __result = filtered;
            return;
        }

        if (TransformOptionUtility.GetInfiniteCard() is not { } infinite)
        {
            return;
        }

        __result = [infinite];
    }
}
