using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace ComicChess.KnowledgeDemon;

internal sealed class AberrantCardCmdDiscardAndDrawPatch : IPatchMethod
{
    public static string PatchId => "knowledgedemon_aberrant_card_cmd_discard_and_draw";
    public static string Description => "Redirect the first status discards each turn back to hand while Aberrant is active";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(CardCmd), nameof(CardCmd.DiscardAndDraw)),
    ];

    public static bool Prefix(
        PlayerChoiceContext choiceContext,
        IEnumerable<CardModel> cardsToDiscard,
        int cardsToDraw,
        ref Task __result)
    {
        __result = DiscardAndDrawImpl(choiceContext, cardsToDiscard, cardsToDraw);
        return false;
    }

    private static async Task DiscardAndDrawImpl(
        PlayerChoiceContext choiceContext,
        IEnumerable<CardModel> cardsToDiscard,
        int cardsToDraw)
    {
        if (CombatManager.Instance.IsOverOrEnding)
        {
            return;
        }

        var discardCards = cardsToDiscard.Where(static c => c != null).Distinct().ToList();
        if (discardCards.Count == 0)
        {
            return;
        }

        var anchor = discardCards.FirstOrDefault(static c => c.Owner != null);
        if (anchor?.Owner is not { } owner)
        {
            return;
        }

        var combatState = anchor.CombatState ?? owner.Creature?.CombatState;
        if (combatState == null)
        {
            return;
        }

        var discardPile = PileType.Discard.GetPile(owner);
        var slyCards = new List<CardModel>();

        foreach (var card in discardCards)
        {
            if (card.Owner?.Creature == null
                || card.HasBeenRemovedFromState
                || card.Owner.Creature.CombatState == null)
            {
                continue;
            }

            if (AberrantDiscardUtility.TryRedirectHandDiscard(card))
            {
                continue;
            }

            if (card.IsSlyThisTurn)
            {
                slyCards.Add(card);
            }

            await CardPileCmd.Add(card, discardPile);
            CombatManager.Instance.History.CardDiscarded(combatState, card);
            await Hook.AfterCardDiscarded(combatState, choiceContext, card);
        }

        discardPile.InvokeContentsChanged();

        if (cardsToDraw > 0)
        {
            await CardPileCmd.Draw(choiceContext, cardsToDraw, owner);
        }

        foreach (var slyCard in slyCards)
        {
            await CardCmd.AutoPlay(choiceContext, slyCard, null, AutoPlayType.SlyDiscard);
        }
    }
}

internal sealed class AberrantCombatManagerFlushPlayerHandPatch : IPatchMethod
{
    public static string PatchId => "knowledgedemon_aberrant_flush_player_hand";
    public static string Description => "Keep the first status cards discarded each turn in hand while Aberrant is active";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(CombatManager), "FlushPlayerHand", [typeof(Player), typeof(HookPlayerChoiceContext)]),
    ];

    public static bool Prefix(
        CombatManager __instance,
        Player player,
        HookPlayerChoiceContext playerChoiceContext,
        ref Task __result)
    {
        _ = __instance;
        __result = FlushPlayerHandImpl(player, playerChoiceContext);
        return false;
    }

    private static async Task FlushPlayerHandImpl(Player player, HookPlayerChoiceContext playerChoiceContext)
    {
        if (player.Creature.IsDead)
        {
            return;
        }

        if (player.Creature.CombatState is not CombatState state || player.PlayerCombatState == null)
        {
            return;
        }

        var cardsToFlush = new List<CardModel>();
        var cardsToRetain = new List<CardModel>();
        var shouldFlush = Hook.ShouldFlush(state, player);
        foreach (var card in PileType.Hand.GetPile(player).Cards.ToList())
        {
            if (!shouldFlush || card.ShouldRetainThisTurn)
            {
                cardsToRetain.Add(card);
            }
            else if (AberrantDiscardUtility.TryRedirectHandDiscard(card))
            {
                cardsToRetain.Add(card);
            }
            else
            {
                cardsToFlush.Add(card);
            }
        }

        if (cardsToFlush.Count > 0)
        {
            await CardPileCmd.Add(cardsToFlush, PileType.Discard);
        }

        await Hook.AfterFlush(state, player, playerChoiceContext, cardsToFlush, cardsToRetain);
        player.PlayerCombatState.EndOfTurnCleanup();
    }
}
