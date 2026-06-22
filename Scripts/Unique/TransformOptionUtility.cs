using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace ComicChess.KnowledgeDemon;

internal static class TransformOptionUtility
{
    internal static IEnumerable<CardModel> GetOtherCharacterPoolCards(Player player, CardPoolModel excludePool)
    {
        return player.UnlockState.CharacterCardPools
            .Where(pool => pool != excludePool)
            .SelectMany(pool => pool.GetUnlockedCards(
                player.UnlockState,
                player.RunState.CardMultiplayerConstraint));
    }

    internal static IEnumerable<CardModel> GetOtherCharacterAndMaybeColorlessPoolCards(
        Player player,
        CardPoolModel excludePool,
        bool includeColorless)
    {
        var cards = GetOtherCharacterPoolCards(player, excludePool);
        if (!includeColorless)
        {
            return cards;
        }

        return cards.Concat(ModelDb.CardPool<ColorlessCardPool>().GetUnlockedCards(
            player.UnlockState,
            player.RunState.CardMultiplayerConstraint));
    }

    internal static IEnumerable<CardModel> FilterTransformCandidates(
        CardModel original,
        IEnumerable<CardModel> candidates,
        bool isInCombat)
    {
        var source = candidates;
        var rarity = original.Rarity;
        if ((uint)(rarity - 8) > 1u)
        {
            source = source.Where(candidate =>
            {
                var candidateRarity = candidate.Rarity;
                return (uint)(candidateRarity - 2) <= 2u;
            });
        }

        if (isInCombat)
        {
            source = source.Where(candidate => candidate.CanBeGeneratedInCombat);
        }

        source = source.Where(candidate => candidate.Id != original.Id);

        if (original.Owner is null)
        {
            return source;
        }

        return FilterForPlayerCount(original.Owner.RunState, source);
    }

    private static IEnumerable<CardModel> FilterForPlayerCount(
        MegaCrit.Sts2.Core.Runs.IRunState runState,
        IEnumerable<CardModel> options)
    {
        if (runState.Players.Count > 1)
        {
            return options.Where(card =>
                card.MultiplayerConstraint != CardMultiplayerConstraint.SingleplayerOnly);
        }

        return options.Where(card =>
            card.MultiplayerConstraint != CardMultiplayerConstraint.MultiplayerOnly);
    }
}
