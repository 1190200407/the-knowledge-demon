using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace ComicChess.KnowledgeDemon;

public static class KnowledgeDemonCardCmd
{
    internal static CardModel? GetRandomKnowledgeDemonStatusCardTemplate(Player player)
    {
        var statusCandidates = TransformOptionUtility.GetKnowledgeDemonStatusPoolCards(player)
            .Where(static card => card.CanBeGeneratedInCombat)
            .DistinctBy(static card => card.Id)
            .ToList();
        if (statusCandidates.Count == 0)
        {
            return null;
        }

        var selectedRarity = RollStatusRarity(player, statusCandidates);
        var rarityCandidates = statusCandidates
            .Where(card => card.Rarity == selectedRarity)
            .ToList();
        if (rarityCandidates.Count == 0)
        {
            rarityCandidates = statusCandidates;
        }

        return player.RunState.Rng.CombatCardGeneration.NextItem(rarityCandidates);
    }

    public static async Task<CardModel?> AddRandomKnowledgeDemonStatusCardToCombat(
        Player player,
        PileType destination = PileType.Hand,
        CardPilePosition position = CardPilePosition.Bottom)
    {
        var canonicalCard = GetRandomKnowledgeDemonStatusCardTemplate(player);
        if (canonicalCard is null)
        {
            return null;
        }

        var combatState = player.Creature.CombatState;
        if (combatState is null)
        {
            return null;
        }

        var generated = combatState.CreateCard(canonicalCard, player);
        await CardPileCmd.AddGeneratedCardToCombat(generated, destination, player, position);
        return generated;
    }

    private static CardRarity RollStatusRarity(Player player, IReadOnlyList<CardModel> candidates)
    {
        var roll = player.RunState.Rng.CombatCardGeneration.NextFloat();
        var preferredRarity = roll < 0.5f
            ? CardRarity.Common
            : roll < 0.9f
                ? CardRarity.Uncommon
                : CardRarity.Rare;

        if (candidates.Any(card => card.Rarity == preferredRarity))
        {
            return preferredRarity;
        }

        return preferredRarity switch
        {
            CardRarity.Common => candidates.Any(card => card.Rarity == CardRarity.Uncommon)
                ? CardRarity.Uncommon
                : CardRarity.Rare,
            CardRarity.Uncommon => candidates.Any(card => card.Rarity == CardRarity.Common)
                ? CardRarity.Common
                : CardRarity.Rare,
            _ => candidates.Any(card => card.Rarity == CardRarity.Uncommon)
                ? CardRarity.Uncommon
                : CardRarity.Common,
        };
    }
}
