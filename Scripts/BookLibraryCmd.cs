using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace ComicChess.KnowledgeDemon;

/// <summary>藏书库：记录临时复制品；抉择展示并自动打出。</summary>
public static class BookLibraryCmd
{
    public const int ChooseOfferCount = 3;

    private static readonly LocString ChooseStartLine =
        MonsterModel.L10NMonsterLookup("KNOWLEDGE_DEMON.moves.CURSE_OF_KNOWLEDGE.startLine");

    private static readonly LocString ChooseDoneLine =
        MonsterModel.L10NMonsterLookup("KNOWLEDGE_DEMON.moves.CURSE_OF_KNOWLEDGE.doneLine");

    #region 记录
    /// <summary>
    /// 将牌的概念记录进藏书库：始终生成临时复制品，不移动原牌。
    /// </summary>
    public static async Task RecordToLibrary(
        PlayerChoiceContext? choiceContext,
        Player player,
        CardModel card,
        int count)
    {
        if (count <= 0 || !BookLibraryUtility.PlayerHasBookLibraryRelic(player))
        {
            return;
        }

        var libraryPileType = BookLibraryUtility.PileType;

        await KnowledgeDemonHook.BeforeRecordedToLibrary(choiceContext, player, card);

        var recordedCopies = new List<CardModel>(count);
        for (var i = 0; i < count; i++)
        {
            var finalTemplate = await KnowledgeDemonHook.ModifyRecordCard(player, card);
            finalTemplate = await KnowledgeDemonHook.ModifyRecordCardLate(player, card, finalTemplate);

            var copy = card.CreateClone();
            await CardPileCmd.AddGeneratedCardToCombat(copy, libraryPileType, player, CardPilePosition.Bottom);

            var recorded = copy;
            if (!RecordTemplatesMatch(card, finalTemplate))
            {
                var replacement = finalTemplate.CreateClone();
                var transformResult = await BookLibraryUtility.TransformCard(copy, replacement);
                recorded = transformResult?.cardAdded ?? replacement;
            }

            recordedCopies.Add(recorded);
        }

        await KnowledgeDemonHook.AfterRecordedToLibrary(choiceContext, player, card, recordedCopies);
    }

    private static bool RecordTemplatesMatch(CardModel source, CardModel finalTemplate) =>
        source.Id == finalTemplate.Id && source.IsUpgraded == finalTemplate.IsUpgraded;

    /// <summary>牌实际进入弃牌堆后，在藏书库留下 1 份临时复制品（仅手动打出且原定进弃牌堆）。</summary>
    public static async Task RecordOnEnteredDiscardPile(CardModel card, PileType oldPileType)
    {
        if (oldPileType != PileType.Play
            || card.Pile is not { Type: PileType.Discard }
            || card.Owner is not Player player
            || !BookLibraryUtility.PlayerHasBookLibraryRelic(player)
            || !WasManuallyPlayedToDiscard(card))
        {
            return;
        }

        await RecordToLibrary(null, player, card, 1);
    }

    private static bool WasManuallyPlayedToDiscard(CardModel card)
    {
        var entry = CombatManager.Instance.History.CardPlaysFinished
            .LastOrDefault(e => e.CardPlay.Card == card);

        return entry is CardPlayFinishedEntry finished
            && !finished.CardPlay.IsAutoPlay
            && finished.CardPlay.ResultPile == PileType.Discard;
    }
    #endregion

    #region 具象
    /// <summary>
    /// 从藏书库中选择至多 N 张牌加入手牌（由玩家选择，不限加入顺序）。
    /// </summary>
    public static async Task<IReadOnlyList<CardModel>> MaterializeFromLibraryToHand(
        PlayerChoiceContext choiceContext,
        Player player,
        int count,
        LocString selectionPrompt,
        AbstractModel? source = null)
    {
        var libraryPile = BookLibraryUtility.TryGetLibraryPile(player);
        if (count <= 0 || libraryPile is null || libraryPile.Cards.Count == 0)
        {
            return [];
        }

        var selectCount = Math.Min(count, libraryPile.Cards.Count);
        var selected = (await KnowledgeDemonCardSelectCmd.FromBookLibrary(
            choiceContext,
            player,
            new CardSelectorPrefs(selectionPrompt, selectCount),
            filter: null,
            source)).ToList();

        var materialized = new List<CardModel>(selected.Count);
        foreach (var card in selected)
        {
            await CardPileCmd.Add(card, PileType.Hand);
            materialized.Add(card);
        }

        await KnowledgeDemonHook.AfterMaterializedFromLibrary(choiceContext, player, materialized);

        return materialized;
    }
    #endregion

    #region 抉择
    public static async Task PlayChooseStartPresentation(Player player)
    {
        TalkCmd.Play(ChooseStartLine, player.Creature, VfxColor.Gold, VfxDuration.Standard);
        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);
    }

    public static void PlayChooseDonePresentation(Player player) =>
        TalkCmd.Play(ChooseDoneLine, player.Creature, VfxColor.Gold, VfxDuration.Standard);

    /// <summary>从藏书库随机展示至多 3 张牌，由玩家选择 1 张。</summary>
    public static async Task<BookLibraryChooseResult> ChooseFromLibrary(
        PlayerChoiceContext choiceContext,
        Player player,
        CardModel? chooseSource = null)
    {
        var libraryPile = BookLibraryUtility.TryGetLibraryPile(player);
        if (libraryPile is null || libraryPile.Cards.Count == 0)
        {
            return BookLibraryChooseResult.Empty;
        }

        var offerCount = Math.Min(ChooseOfferCount, libraryPile.Cards.Count);
        var candidates = libraryPile.Cards
            .ToList()
            .StableShuffle(player.RunState.Rng.Shuffle)
            .Take(offerCount)
            .ToList();

        return await ChooseFromCandidates(choiceContext, player, candidates, chooseSource);
    }

    /// <summary>从指定候选中由玩家选择 1 张。</summary>
    public static async Task<BookLibraryChooseResult> ChooseFromCandidates(
        PlayerChoiceContext choiceContext,
        Player player,
        IReadOnlyList<CardModel> candidates,
        CardModel? chooseSource = null)
    {
        if (candidates.Count == 0)
        {
            return BookLibraryChooseResult.Empty;
        }

        var candidateList = candidates.ToList();

        await KnowledgeDemonHook.BeforeChooseFromLibrary(choiceContext, player, chooseSource, candidateList);

        await ExtractCandidatesFromLibrary(player, candidateList);

        try
        {
            KnowledgeDemonChooseContext.Begin(candidateList, player);

            var chosen = await CardSelectCmd.FromChooseACardScreen(choiceContext, candidateList, player);

            await KnowledgeDemonHook.AfterChooseFromLibrary(
                choiceContext,
                player,
                chooseSource,
                chosen,
                candidateList);

            return new BookLibraryChooseResult(chosen, candidateList);
        }
        finally
        {
            KnowledgeDemonChooseContext.End();
        }
    }

    /// <summary>抉择并自动结算（<see cref="ChooseFromLibrary" /> + <see cref="ApplyChooseResult" />）。</summary>
    public static async Task ChooseFromLibraryAndAutoPlay(
        PlayerChoiceContext choiceContext,
        Player player,
        CardModel? chooseSource = null)
    {
        var result = await ChooseFromLibrary(choiceContext, player, chooseSource);
        await ApplyChooseResult(choiceContext, player, result);
    }

    /// <summary>从指定候选中抉择 1 张并自动结算。</summary>
    public static async Task ChooseFromCandidatesAndAutoPlay(
        PlayerChoiceContext choiceContext,
        Player player,
        IReadOnlyList<CardModel> candidates,
        CardModel? chooseSource = null)
    {
        var result = await ChooseFromCandidates(choiceContext, player, candidates, chooseSource);
        await ApplyChooseResult(choiceContext, player, result);
    }

    /// <summary>结算抉择：选中牌自动打出并消失；未选候选按奇巧规则处理并消失。</summary>
    public static async Task ApplyChooseResult(
        PlayerChoiceContext choiceContext,
        Player player,
        BookLibraryChooseResult result)
    {
        if (!result.HasCandidates)
        {
            return;
        }

        if (result.Chosen != null)
        {
            KnowledgeDemonChooseContext.ClearChoosePreviewFlag(result.Chosen);
            await CardCmd.AutoPlay(choiceContext, result.Chosen, null);
            await TryVanishFromLibrary(result.Chosen);
        }

        await ResolveUnchosenLibraryCandidates(choiceContext, result.Unchosen);
    }

    /// <summary>未抉择的候选：奇巧则自动打出，之后从战斗中消失。</summary>
    public static async Task ResolveUnchosenLibraryCandidates(
        PlayerChoiceContext choiceContext,
        IReadOnlyList<CardModel> unchosen)
    {
        foreach (var card in unchosen)
        {
            KnowledgeDemonChooseContext.ClearChoosePreviewFlag(card);
            if (card.IsSlyThisTurn)
            {
                await CardCmd.AutoPlay(choiceContext, card, null, AutoPlayType.SlyDiscard);
            }

            await TryVanishFromLibrary(card);
        }
    }

    /// <summary>抉择展示前将候选移出藏书库（仍留在战斗内，供选择与自动打出）。</summary>
    private static async Task ExtractCandidatesFromLibrary(Player player, IReadOnlyList<CardModel> candidates)
    {
        var combatState = player.Creature.CombatState;
        if (combatState is null)
        {
            return;
        }

        foreach (var card in candidates)
        {
            if (!IsInBookLibrary(card))
            {
                continue;
            }

            var oldPileType = card.Pile!.Type;
            card.RemoveFromCurrentPile(silent: false);
            BookLibraryUtility.ResetCardTint(card);
            await Hook.AfterCardChangedPiles(player.RunState, combatState, card, oldPileType, null);
        }
    }

    private static bool IsInBookLibrary(CardModel card) =>
        card.Pile is { } pile && BookLibraryUtility.IsBookLibraryPile(pile.Type);
    #endregion

    #region 移除
    private static async Task TryVanishFromLibrary(CardModel card)
    {
        if (card.HasBeenRemovedFromState)
        {
            return;
        }

        if (card.Pile is { IsCombatPile: true })
        {
            await CardPileCmd.RemoveFromCombat(card, skipVisuals: true);
            return;
        }

        card.RemoveFromState();
    }

    private static async Task VanishFromLibrary(CardModel card) =>
        await TryVanishFromLibrary(card);

    private static async Task VanishFromLibrary(IReadOnlyList<CardModel> cards)
    {
        foreach (var card in cards)
        {
            await TryVanishFromLibrary(card);
        }
    }

    /// <summary>回合结束前移除藏书库内全部临时复制品（视为消失，不进弃牌堆）。</summary>
    public static async Task DismissLibraryAtTurnEnd(PlayerChoiceContext choiceContext, Player player)
    {
        if (!BookLibraryUtility.PlayerHasBookLibraryRelic(player))
        {
            return;
        }

        var libraryPile = BookLibraryUtility.PileType.GetPile(player);
        if (libraryPile.Cards.Count == 0)
        {
            return;
        }

        var copies = libraryPile.Cards.ToList();
        await CardPileCmd.RemoveFromCombat(copies, skipVisuals: false);
    }
    #endregion
}
