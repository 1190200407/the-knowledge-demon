using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace ComicChess.KnowledgeDemon;

internal static class TransformOptionUtility
{
    internal static IEnumerable<CardModel> GetInfiniteTransformationStatusCandidates(Player player)
    {
        return FilterForPlayerCount(
                player.RunState,
                ModelDb.CardPool<StatusCardPool>().GetUnlockedCards(
                    player.UnlockState,
                    player.RunState.CardMultiplayerConstraint)
                    .Concat(GetKnowledgeDemonStatusPoolCards(player)))
            .Where(card => card.Type == CardType.Status && !IsInfinite(card))
            .GroupBy(card => card.Id)
            .Select(group => group.First());
    }

    internal static IEnumerable<CardModel> GetKnowledgeDemonStatusPoolCards(Player player)
    {
        return ModelDb.CardPool<KnowledgeDemonCardPool>()
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Where(card => card.Type == CardType.Status && !IsInfinite(card));
    }

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
        var source = candidates.Where(card => !IsInfinite(card));
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

    internal static IEnumerable<CardModel> GetAllCardsTransformCandidates(Player player, CardModel original)
    {
        return FilterForPlayerCount(
            player.RunState,
            ModelDb.AllCards.Where(card =>
                card.Id != original.Id
                && !IsInfinite(card)
                && card.CanBeGeneratedInCombat
                && card.Rarity != CardRarity.Token
                && card.Rarity != CardRarity.Event
                && card.Type != CardType.Quest));
    }

    internal static CardModel? GetInfiniteCard() =>
        ModelDb.AllCards.FirstOrDefault(IsInfinite);

    internal static bool IsInfinite(CardModel card) => card is Infinite;

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
