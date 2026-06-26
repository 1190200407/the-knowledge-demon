using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;

namespace ComicChess.KnowledgeDemon;

public static class KnowledgeDemonUniqueUtility
{
    private const int MaxDeckTransformAttempts = 32;

    private static readonly HashSet<ModelId> DuplicateCardIdScratch = new();
    private static readonly HashSet<CardModel> CombatScopeScratch = new();

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

        CombatScopeScratch.Clear();
        var cards = new List<CardModel>();

        foreach (var card in combatState.AllCards)
        {
            if (CombatScopeScratch.Add(card))
            {
                cards.Add(card);
            }
        }

        return cards;
    }

    public static bool ShouldTransformAsDuplicate(Player player, CardModel card) =>
        IsUnique(card) && CombatContainsSameUnique(player, card, card);

    public static bool HasDuplicateCardIdInHandOrLibrary(Player player)
    {
        DuplicateCardIdScratch.Clear();

        foreach (var card in PileType.Hand.GetPile(player).Cards)
        {
            if (!DuplicateCardIdScratch.Add(card.Id))
            {
                return true;
            }
        }

        var libraryPile = BookLibraryUtility.TryGetLibraryPile(player);
        if (libraryPile is not null)
        {
            foreach (var card in libraryPile.Cards)
            {
                if (!DuplicateCardIdScratch.Add(card.Id))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static CardModel PreferNewerDuplicate(CardModel first, CardModel second)
    {
        if (first.Pile is not { } firstPile || second.Pile is not { } secondPile)
        {
            return second;
        }

        if (firstPile.Type == PileType.Deck && secondPile.Type == PileType.Deck)
        {
            var firstIndex = IndexInPile(firstPile, first);
            var secondIndex = IndexInPile(secondPile, second);
            return secondIndex > firstIndex ? second : first;
        }

        var firstOrder = GetCombatEntryOrder(first);
        var secondOrder = GetCombatEntryOrder(second);
        return secondOrder >= firstOrder ? second : first;
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
                return CreateStatePreservingReplacement(card, candidate);
            }

            if (!candidate.IsTransformable)
            {
                return CreateStatePreservingReplacement(card, candidate);
            }

            var replacement = new CardTransformation(candidate).GetReplacement(rng);
            if (replacement == null)
            {
                return CreateStatePreservingReplacement(card, candidate);
            }

            candidate = replacement;
        }

        return CreateStatePreservingReplacement(card, candidate);
    }

    public static CardModel CreateStatePreservingReplacement(CardModel source, CardModel replacement)
    {
        if (ReferenceEquals(source, replacement))
        {
            return replacement;
        }

        PreserveUpgradeLevel(source, replacement);
        PreserveEnchantment(source, replacement);
        return replacement;
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

    private static long GetCombatEntryOrder(CardModel card)
    {
        var history = CombatManager.Instance?.History;
        if (history is null)
        {
            return 0;
        }

        long order = 0;
        long current = 1;
        foreach (var entry in history.Entries.OfType<CardGeneratedEntry>())
        {
            if (ReferenceEquals(entry.Card, card))
            {
                order = current;
            }

            current++;
        }

        return order;
    }

    private static void PreserveUpgradeLevel(CardModel source, CardModel replacement)
    {
        while (replacement.CurrentUpgradeLevel < source.CurrentUpgradeLevel && replacement.IsUpgradable)
        {
            replacement.UpgradeInternal();
            replacement.FinalizeUpgradeInternal();
        }
    }

    private static void PreserveEnchantment(CardModel source, CardModel replacement)
    {
        if (source.Enchantment is not { } sourceEnchantment || replacement.Enchantment != null)
        {
            return;
        }

        var clonedEnchantment = (EnchantmentModel)sourceEnchantment.ClonePreservingMutability();
        if (!clonedEnchantment.CanEnchant(replacement))
        {
            return;
        }

        replacement.EnchantInternal(clonedEnchantment, clonedEnchantment.Amount);
        clonedEnchantment.ModifyCard();
    }

}
