using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace ComicChess.KnowledgeDemon;

internal static class TransformOptionResolver
{
    internal static CardModel[] ResolveCandidates(
        Player player,
        CardModel original,
        bool isInCombat,
        IEnumerable<CardModel> baseCandidates)
    {
        IEnumerable<CardModel> candidates = baseCandidates ?? [];
        if (original.Type == CardType.Status
            && player.Creature.GetPower<EnvironmentalTolerancePower>() is not null)
        {
            candidates = TransformOptionUtility.GetEnvironmentalToleranceTransformCandidates(
                player,
                original,
                isInCombat);
        }

        var merged = candidates.ToList();
        var existingIds = merged.Select(card => card.Id).ToHashSet();

        foreach (var provider in EnumerateProviders(player))
        {
            var additional = TransformOptionUtility.FilterTransformCandidates(
                original,
                provider.GetAdditionalTransformCandidates(player, original, isInCombat),
                isInCombat);

            foreach (var candidate in additional)
            {
                if (existingIds.Add(candidate.Id))
                {
                    merged.Add(candidate);
                }
            }
        }

        var filtered = merged
            .Where(candidate => isInCombat
                ? !KnowledgeDemonUniqueUtility.WouldViolateCombatUniqueRule(player, candidate, original)
                : !KnowledgeDemonUniqueUtility.WouldViolateDeckUniqueRule(player, candidate, original))
            .GroupBy(card => card.Id)
            .Select(group => group.First())
            .ToArray();

        if (filtered.Length > 0)
        {
            return filtered;
        }

        return TransformOptionUtility.GetInfiniteCard() is { } infinite ? [infinite] : [];
    }

    private static IEnumerable<ITransformOptionCandidateProvider> EnumerateProviders(Player player)
    {
        foreach (var relic in player.Relics.OfType<ITransformOptionCandidateProvider>())
        {
            yield return relic;
        }

        foreach (var power in player.Creature.Powers.OfType<ITransformOptionCandidateProvider>())
        {
            yield return power;
        }
    }
}
