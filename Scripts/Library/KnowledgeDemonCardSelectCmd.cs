using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ComicChess.KnowledgeDemon;

/// <summary>
/// 藏书库选牌 API，形态对齐原版 <c>CardSelectCmd.FromHand</c> / <c>FromCombatPile</c>。
/// 无法在原版 <c>CardSelectCmd</c> 上直接追加方法（跨程序集同名类型），故集中在此。
/// </summary>
public static class KnowledgeDemonCardSelectCmd
{
    public static Task<IEnumerable<CardModel>> FromBookLibrary(
        PlayerChoiceContext context,
        Player player,
        CardSelectorPrefs prefs,
        Func<CardModel, bool>? filter = null,
        AbstractModel? source = null) =>
        SelectFromSources(context, player, prefs, filter, includeHand: false, source);

    public static Task<IEnumerable<CardModel>> FromBookLibraryAndHand(
        PlayerChoiceContext context,
        Player player,
        CardSelectorPrefs prefs,
        Func<CardModel, bool>? filter = null,
        AbstractModel? source = null) =>
        SelectFromSources(context, player, prefs, filter, includeHand: true, source);

    private static async Task<IEnumerable<CardModel>> SelectFromSources(
        PlayerChoiceContext context,
        Player player,
        CardSelectorPrefs prefs,
        Func<CardModel, bool>? filter,
        bool includeHand,
        AbstractModel? source)
    {
        if (CombatManager.Instance.IsOverOrEnding)
        {
            return [];
        }

        filter ??= _ => true;

        List<CardModel> libraryCards = [];
        if (BookLibraryUtility.PlayerHasBookLibraryAccess(player))
        {
            var libraryPile = BookLibraryUtility.TryGetLibraryPile(player);
            if (libraryPile is not null)
            {
                libraryCards = libraryPile.Cards.Where(filter).ToList();
            }
        }

        var handCards = includeHand
            ? PileType.Hand.GetPile(player).Cards.Where(filter).ToList()
            : [];

        var candidates = libraryCards.Concat(handCards).ToList();
        if (candidates.Count == 0)
        {
            return [];
        }

        if (!prefs.RequireManualConfirmation && candidates.Count <= prefs.MinSelect)
        {
            return candidates;
        }

        if (includeHand && libraryCards.Count == 0 && handCards.Count > 0 && source is not null)
        {
            return await CardSelectCmd.FromHand(context, player, prefs, filter, source);
        }

        if (!includeHand && libraryCards.Count == 0)
        {
            return [];
        }

        if (CardSelectCmd.Selector != null)
        {
            return await CardSelectCmd.Selector.GetSelectedCards(candidates, prefs.MinSelect, prefs.MaxSelect);
        }

        uint choiceId = RunManager.Instance.PlayerChoiceSynchronizer.ReserveChoiceId(player);
        await context.SignalPlayerChoiceBegun(player, PlayerChoiceOptions.CancelPlayCardActions);

        IEnumerable<CardModel> result;
        if (ShouldSelectLocalCard(player))
        {
            NPlayerHand.Instance?.CancelAllCardPlay();
            var pile = NBookLibraryPile.Instance
                ?? throw new InvalidOperationException("Book library pile is not ready for card selection.");
            result = await pile.RunSession(prefs, filter, includeHand);
            RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(
                player,
                choiceId,
                PlayerChoiceResult.FromMutableCombatCards(result));
        }
        else
        {
            result = (await RunManager.Instance.PlayerChoiceSynchronizer.WaitForRemoteChoice(player, choiceId))
                .AsCombatCards();
        }

        await context.SignalPlayerChoiceEnded();
        return result;
    }

    private static bool ShouldSelectLocalCard(Player player) =>
        LocalContext.IsMe(player) && RunManager.Instance.NetService.Type != NetGameType.Replay;
}
