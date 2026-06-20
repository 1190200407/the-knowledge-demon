using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;

namespace ComicChess.KnowledgeDemon;

public static class KnowledgeDemonUniqueUtility
{
    private const int MaxDeckTransformAttempts = 32;

    private static CardKeyword UniqueKeyword =>
        ModKeywordRegistry.GetCardKeyword(KnowledgeDemonKeyword.Unique);

    public static bool IsUnique(CardModel card) => card.HasModKeyword(UniqueKeyword);

    public static bool SharesUniqueName(CardModel left, CardModel right) => left.Id == right.Id;

    public static bool DeckContainsSameUnique(Player player, CardModel card) =>
        IsUnique(card)
        && PileType.Deck.GetPile(player).Cards.Any(deckCard => SharesUniqueName(deckCard, card));

    public static bool CombatContainsSameUnique(Player player, CardModel card, CardModel? except = null) =>
        IsUnique(card)
        && IterateCombatUniqueScope(player).Any(
            combatCard => combatCard != except && SharesUniqueName(combatCard, card));

    public static IEnumerable<CardModel> IterateCombatUniqueScope(Player player)
    {
        if (player.PlayerCombatState is not { } combatState)
        {
            return [];
        }

        var cards = combatState.AllCards.ToList();
        var libraryPile = BookLibraryUtility.TryGetLibraryPile(player);
        if (libraryPile is not null)
        {
            cards.AddRange(libraryPile.Cards);
        }

        return cards;
    }

    public static bool ShouldTransformAsDuplicate(Player player, CardModel card) =>
        IsUnique(card) && CombatContainsSameUnique(player, card, card);

    public static CardModel PreferNewerDuplicate(CardModel first, CardModel second)
    {
        if (first.Pile is not { } firstPile || second.Pile is not { } secondPile)
        {
            return second;
        }

        if (ReferenceEquals(firstPile, secondPile))
        {
            var firstIndex = IndexInPile(firstPile, first);
            var secondIndex = IndexInPile(secondPile, second);
            return secondIndex > firstIndex ? second : first;
        }

        return second;
    }

    public static bool WouldViolateDeckUniqueRule(Player player, CardModel candidate, CardModel? replaced = null) =>
        IsUnique(candidate)
        && PileType.Deck.GetPile(player).Cards.Any(c => c != replaced && SharesUniqueName(c, candidate));

    public static bool WouldViolateCombatUniqueRule(Player player, CardModel candidate, CardModel? replaced = null) =>
        CombatContainsSameUnique(player, candidate, replaced);

    public static CardModel ResolveDeckAddition(Player player, CardModel card)
    {
        if (!IsUnique(card) || !DeckContainsSameUnique(player, card))
        {
            return card;
        }

        var rng = player.RunState.Rng.Niche;
        var candidate = card;

        for (var attempt = 0; attempt < MaxDeckTransformAttempts; attempt++)
        {
            if (!WouldViolateDeckUniqueRule(player, candidate))
            {
                return candidate;
            }

            if (!candidate.IsTransformable)
            {
                return candidate;
            }

            var replacement = new CardTransformation(candidate).GetReplacement(rng);
            if (replacement == null)
            {
                return candidate;
            }

            candidate = replacement;
        }

        return candidate;
    }

    private static int IndexInPile(CardPile pile, CardModel card)
    {
        for (var i = 0; i < pile.Cards.Count; i++)
        {
            if (pile.Cards[i] == card)
            {
                return i;
            }
        }

        return -1;
    }
}
