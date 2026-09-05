using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class ShiftPerspective : KnowledgeDemonCardModel
{
    private const int energyCost = 1;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Common;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.SingleplayerOnly;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move),
    ];

    public ShiftPerspective()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target is not null)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_blunt")
                .Execute(choiceContext);
        }

        var selected = (await KnowledgeDemonCardSelectCmd.FromBookLibraryAndHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 2),
            source: this)).ToList();

        if (selected.Count == 2)
        {
            await SwapSelectedCardPositions(Owner, selected[0], selected[1]);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }

    private static async Task SwapSelectedCardPositions(Player player, CardModel first, CardModel second)
    {
        if (ReferenceEquals(first, second)
            || first.Owner != player
            || second.Owner != player
            || first.Pile is not { } firstPile
            || second.Pile is not { } secondPile
            || !IsSwappablePile(firstPile.Type)
            || !IsSwappablePile(secondPile.Type))
        {
            return;
        }

        var firstIndex = IndexOfCard(firstPile.Cards, first);
        var secondIndex = IndexOfCard(secondPile.Cards, second);
        if (firstIndex < 0 || secondIndex < 0)
        {
            return;
        }

        if (ReferenceEquals(firstPile, secondPile))
        {
            if (firstPile.Type == PileType.Hand)
            {
                await SwapBetweenHandAndHand(player, first, firstIndex, second, secondIndex);
                return;
            }

            await SwapBetweenLibraryAndLibrary(firstPile, first, firstIndex, second, secondIndex);
            return;
        }

        var combatState = player.Creature.CombatState;
        await SwapBetweenHandAndLibrary(
            player,
            combatState,
            first,
            firstPile,
            firstIndex,
            second,
            secondPile,
            secondIndex);
    }

    private static bool IsSwappablePile(PileType pileType) =>
        pileType == PileType.Hand || BookLibraryUtility.IsBookLibraryPile(pileType);

    private static void RefreshMovedCardVisuals(CardModel card)
    {
        if (card.Pile is not { } pile)
        {
            return;
        }

        if (pile.Type == PileType.Hand)
        {
            BookLibraryUtility.RefreshHandCardVisual(card);
            return;
        }

        if (BookLibraryUtility.IsBookLibraryPile(pile.Type))
        {
            BookLibraryUtility.RefreshCardVisual(card);
            NBookLibraryPile.Instance?.ArrangeCards(animate: true);
        }
    }

    private static async Task SwapBetweenLibraryAndLibrary(
        CardPile libraryPile,
        CardModel first,
        int firstIndex,
        CardModel second,
        int secondIndex)
    {
        if (firstIndex == secondIndex)
        {
            return;
        }

        first.RemoveFromCurrentPile(silent: true);
        second.RemoveFromCurrentPile(silent: true);
        libraryPile.AddInternal(second, firstIndex, silent: true);
        libraryPile.AddInternal(first, secondIndex, silent: true);

        BookLibraryUtility.RefreshCardVisual(first);
        BookLibraryUtility.RefreshCardVisual(second);
        NBookLibraryPile.Instance?.ArrangeCards(animate: true);
        await Task.CompletedTask;
    }

    private static async Task SwapBetweenHandAndHand(
        Player player,
        CardModel first,
        int firstIndex,
        CardModel second,
        int secondIndex)
    {
        if (firstIndex == secondIndex)
        {
            return;
        }

        var hand = NCombatRoom.Instance?.Ui?.Hand;
        if (hand == null)
        {
            return;
        }

        var firstHolder = hand.GetCardHolder(first);
        var secondHolder = hand.GetCardHolder(second);
        var firstCardNode = firstHolder?.CardNode;
        var secondCardNode = secondHolder?.CardNode;
        if (firstCardNode == null || secondCardNode == null)
        {
            return;
        }

        hand.Remove(first);
        hand.Remove(second);
        first.RemoveFromCurrentPile(silent: true);
        second.RemoveFromCurrentPile(silent: true);

        PileType.Hand.GetPile(player).AddInternal(second, firstIndex, silent: true);
        PileType.Hand.GetPile(player).AddInternal(first, secondIndex, silent: true);

        hand.Add(secondCardNode, firstIndex);
        hand.Add(firstCardNode, secondIndex);
        BookLibraryUtility.RefreshHandCardVisual(second);
        BookLibraryUtility.RefreshHandCardVisual(first);
        hand.ForceRefreshCardIndices();
        await Task.CompletedTask;
    }

    private static async Task SwapBetweenHandAndLibrary(
        Player player,
        MegaCrit.Sts2.Core.Combat.ICombatState? combatState,
        CardModel first,
        CardPile firstPile,
        int firstIndex,
        CardModel second,
        CardPile secondPile,
        int secondIndex)
    {
        var hand = NCombatRoom.Instance?.Ui?.Hand;
        var libraryUi = NBookLibraryPile.Instance;
        if (hand == null || libraryUi == null)
        {
            return;
        }

        var handCard = firstPile.Type == PileType.Hand ? first : second;
        var handIndex = firstPile.Type == PileType.Hand ? firstIndex : secondIndex;
        var libraryCard = firstPile.Type == PileType.Hand ? second : first;
        var libraryIndex = firstPile.Type == PileType.Hand ? secondIndex : firstIndex;
        var handHolder = hand.GetCardHolder(handCard);
        var handCardNode = handHolder?.CardNode;
        var libraryCardNode = libraryUi.TakeCardForSelection(libraryCard);
        if (handCardNode == null || libraryCardNode == null)
        {
            if (libraryCardNode != null)
            {
                libraryUi.RestoreSelectionVisual(libraryCard, libraryCardNode);
            }

            return;
        }

        hand.Remove(handCard);
        handCard.RemoveFromCurrentPile(silent: true);
        libraryCard.RemoveFromCurrentPile(silent: true);

        BookLibraryUtility.ResetCardTint(handCard);
        BookLibraryUtility.ResetCardTint(libraryCard);

        BookLibraryUtility.PileType.GetPile(player).AddInternal(handCard, libraryIndex, silent: true);
        PileType.Hand.GetPile(player).AddInternal(libraryCard, handIndex, silent: true);

        libraryUi.RestoreSelectionVisual(handCard, handCardNode);
        hand.Add(libraryCardNode, handIndex);
        BookLibraryUtility.RefreshHandCardVisual(libraryCard);
        BookLibraryUtility.RefreshCardVisual(handCard);

        if (combatState != null)
        {
            await Hook.AfterCardChangedPiles(player.RunState, combatState, handCard, PileType.Hand, null);
            await Hook.AfterCardChangedPiles(player.RunState, combatState, libraryCard, BookLibraryUtility.PileType, null);
        }
    }

    private static int IndexOfCard(IReadOnlyList<CardModel> cards, CardModel target)
    {
        for (var i = 0; i < cards.Count; i++)
        {
            if (cards[i] == target)
            {
                return i;
            }
        }

        return -1;
    }
}
