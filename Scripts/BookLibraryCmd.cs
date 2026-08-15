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
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace ComicChess.KnowledgeDemon;

public static class BookLibraryCmd
{
    private static readonly LocString ChooseStartLine =
        MonsterModel.L10NMonsterLookup("KNOWLEDGE_DEMON.moves.CURSE_OF_KNOWLEDGE.startLine");

    private static readonly LocString ChooseDoneLine =
        MonsterModel.L10NMonsterLookup("KNOWLEDGE_DEMON.moves.CURSE_OF_KNOWLEDGE.doneLine");

    #region Record
    public static async Task RecordToLibrary(
        PlayerChoiceContext? choiceContext,
        Player player,
        CardModel card,
        int count)
    {
        if (count <= 0 || !BookLibraryUtility.PlayerHasBookLibraryAccess(player))
        {
            return;
        }

        if (!await KnowledgeDemonHook.CanRecordToLibrary(choiceContext, player, card))
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

        foreach (var recordedGroup in recordedCopies.GroupBy(recorded => recorded.Id))
        {
            KnowledgeDemonHistory.LogRecorded(
                player,
                card,
                recordedGroup.Key,
                recordedGroup.Count());
        }

        await KnowledgeDemonHook.AfterRecordedToLibrary(choiceContext, player, card, recordedCopies);
        KnowledgeDemonTelemetryEvents.CaptureRecordedToLibrary(player, card, recordedCopies);
    }

    private static bool RecordTemplatesMatch(CardModel source, CardModel finalTemplate) =>
        source.Id == finalTemplate.Id && source.IsUpgraded == finalTemplate.IsUpgraded;

    public static async Task RecordOnEnteredDiscardPile(
        PlayerChoiceContext? choiceContext,
        CardModel card,
        PileType oldPileType)
    {
        if (oldPileType != PileType.Play
            || card.Pile is not { Type: PileType.Discard }
            || card.Owner is not Player player
            || !BookLibraryUtility.PlayerHasBookLibraryAccess(player)
            || !WasManuallyPlayedToDiscard(card))
        {
            return;
        }

        await RecordToLibrary(choiceContext, player, card, 1);
    }

    private static bool WasManuallyPlayedToDiscard(CardModel card)
    {
        var entry = CombatManager.Instance.History.CardPlaysFinished
            .LastOrDefault(e => e.CardPlay.Card == card);

        return entry is CardPlayFinishedEntry finished
            && !finished.CardPlay.IsAutoPlay
            && finished.CardPlay.ResultPile == PileType.Discard;
    }

    private static bool WasManuallyPlayed(CardModel card)
    {
        var entry = CombatManager.Instance.History.CardPlaysFinished
            .LastOrDefault(e => e.CardPlay.Card == card);

        return entry is CardPlayFinishedEntry finished
            && !finished.CardPlay.IsAutoPlay;
    }
    #endregion

    #region Materialize
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
            var resultCard = source is CardModel sourceCard
                ? await KnowledgeDemonHook.ModifyMaterializeCard(player, sourceCard, card)
                : card;

            if (ReferenceEquals(resultCard, card))
            {
                await CardPileCmd.Add(card, PileType.Hand);
                BookLibraryUtility.RefreshHandCardVisual(card);
            }
            else
            {
                await CardPileCmd.AddGeneratedCardToCombat(resultCard, PileType.Hand, player);
                BookLibraryUtility.RefreshHandCardVisual(resultCard);
            }

            materialized.Add(resultCard);
        }

        await KnowledgeDemonHook.AfterMaterializedFromLibrary(choiceContext, player, materialized);
        KnowledgeDemonTelemetryEvents.CaptureMaterializedFromLibrary(
            player,
            source as CardModel,
            selected,
            materialized);

        return materialized;
    }
    #endregion

    #region Discard
    public static async Task<IReadOnlyList<CardModel>> DiscardFromLibraryAndHand(
        PlayerChoiceContext choiceContext,
        Player player,
        int count,
        LocString selectionPrompt,
        AbstractModel? source = null)
    {
        if (count <= 0)
        {
            return [];
        }

        var selected = (await KnowledgeDemonCardSelectCmd.FromBookLibraryAndHand(
            choiceContext,
            player,
            new CardSelectorPrefs(selectionPrompt, 0, count),
            filter: null,
            source)).ToList();

        if (selected.Count == 0)
        {
            return [];
        }

        var handCards = selected
            .Where(card => card.Pile?.Type == PileType.Hand)
            .ToList();
        var libraryCards = selected
            .Where(card => card.Pile is { } pile && BookLibraryUtility.IsBookLibraryPile(pile.Type))
            .ToList();

        if (handCards.Count > 0)
        {
            await CardCmd.Discard(choiceContext, handCards);
        }

        if (libraryCards.Count > 0)
        {
            await DiscardFromLibrary(choiceContext, player, libraryCards);
        }

        return selected;
    }

    private static async Task DiscardFromLibrary(
        PlayerChoiceContext choiceContext,
        Player player,
        IReadOnlyList<CardModel> cards)
    {
        var combatState = player.Creature.CombatState;
        if (combatState is null)
        {
            return;
        }

        foreach (var card in cards)
        {
            if (card.Pile is not { } pile || !BookLibraryUtility.IsBookLibraryPile(pile.Type))
            {
                continue;
            }

            if (await AberrantDiscardUtility.TryRedirectLibraryDiscard(card))
            {
                continue;
            }

            var oldPileType = pile.Type;
            card.RemoveFromCurrentPile(silent: false);
            BookLibraryUtility.ResetCardTint(card);
            await Hook.AfterCardChangedPiles(player.RunState, combatState, card, oldPileType, null);

            CombatManager.Instance.History.CardDiscarded(combatState, card);
            await Hook.AfterCardDiscarded(combatState, choiceContext, card);

            if (card.IsSlyThisTurn)
            {
                await CardCmd.AutoPlay(choiceContext, card, null, AutoPlayType.SlyDiscard);
            }

            await TryVanishFromLibrary(card);
        }
    }
    #endregion

    #region Choose
    public static async Task PlayChooseStartPresentation(Player player)
    {
        TalkCmd.Play(ChooseStartLine, player.Creature, VfxColor.Gold, VfxDuration.Standard);
        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);
    }

    public static void PlayChooseDonePresentation(Player player) =>
        TalkCmd.Play(ChooseDoneLine, player.Creature, VfxColor.Gold, VfxDuration.Standard);

    public static async Task<BookLibraryChooseResult> ChooseFromLibrary(
        PlayerChoiceContext choiceContext,
        Player player,
        CardModel? chooseSource = null,
        int chooseOfferCount = 3)
    {
        var libraryPile = BookLibraryUtility.TryGetLibraryPile(player);
        if (libraryPile is null || libraryPile.Cards.Count == 0)
        {
            return BookLibraryChooseResult.Empty;
        }

        var choosableCards = libraryPile.Cards.ToList();
        if (choosableCards.Count == 0)
        {
            return BookLibraryChooseResult.Empty;
        }

        var offerCount = Math.Min(chooseOfferCount, choosableCards.Count);
        var candidates = choosableCards
            .StableShuffle(player.RunState.Rng.Shuffle)
            .Take(offerCount)
            .ToList();

        return await ChooseFromCandidates(choiceContext, player, candidates, chooseSource);
    }

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
            if (chosen != null)
            {
                KnowledgeDemonHistory.LogChoose(player, chooseSource, chosen);
            }

            await KnowledgeDemonHook.AfterChooseFromLibrary(
                choiceContext,
                player,
                chooseSource,
                chosen,
                candidateList);
            KnowledgeDemonTelemetryEvents.CaptureChooseResolved(player, chooseSource, chosen, candidateList);

            return new BookLibraryChooseResult(chosen, candidateList);
        }
        finally
        {
            KnowledgeDemonChooseContext.End();
        }
    }

    public static async Task ChooseFromLibraryAndAutoPlay(
        PlayerChoiceContext choiceContext,
        Player player,
        CardModel? chooseSource = null,
        int chooseOfferCount = 3)
    {
        var result = await ChooseFromLibrary(choiceContext, player, chooseSource, chooseOfferCount);
        await ApplyChooseResult(choiceContext, player, result);
    }

    public static async Task ChooseFromCandidatesAndAutoPlay(
        PlayerChoiceContext choiceContext,
        Player player,
        IReadOnlyList<CardModel> candidates,
        CardModel? chooseSource = null)
    {
        var result = await ChooseFromCandidates(choiceContext, player, candidates, chooseSource);
        await ApplyChooseResult(choiceContext, player, result);
    }

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

    public static async Task ResolveUnchosenLibraryCandidates(
        PlayerChoiceContext choiceContext,
        IReadOnlyList<CardModel> unchosen)
    {
        if (unchosen.Count == 0)
        {
            return;
        }

        var player = unchosen.FirstOrDefault(static card => card.Owner != null)?.Owner;
        if (player == null)
        {
            foreach (var card in unchosen)
            {
                KnowledgeDemonChooseContext.ClearChoosePreviewFlag(card);
                await TryVanishFromLibrary(card);
            }

            return;
        }

        foreach (var card in unchosen)
        {
            KnowledgeDemonChooseContext.ClearChoosePreviewFlag(card);
        }

        await DiscardExtractedLibraryCards(choiceContext, player, unchosen);
    }

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

    private static async Task DiscardExtractedLibraryCards(
        PlayerChoiceContext choiceContext,
        Player player,
        IReadOnlyList<CardModel> cards)
    {
        var combatState = player.Creature.CombatState;
        if (combatState is null)
        {
            return;
        }

        foreach (var card in cards)
        {
            if (card.HasBeenRemovedFromState)
            {
                continue;
            }

            if (await AberrantDiscardUtility.TryRedirectLibraryDiscard(card))
            {
                continue;
            }

            CombatManager.Instance.History.CardDiscarded(combatState, card);
            await Hook.AfterCardDiscarded(combatState, choiceContext, card);

            if (card.IsSlyThisTurn)
            {
                await CardCmd.AutoPlay(choiceContext, card, null, AutoPlayType.SlyDiscard);
            }

            await TryVanishFromLibrary(card);
        }
    }
    #endregion

    #region Duplicate
    public static async Task<CardModel?> DuplicateCardToCurrentPile(
        Player player,
        CardModel source,
        CardPilePosition position = CardPilePosition.Bottom)
    {
        if (source.Pile is not { } pile)
        {
            return null;
        }

        var duplicate = source.CreateClone();
        if (pile.Type == PileType.Hand)
        {
            await CardPileCmd.AddGeneratedCardToCombat(duplicate, PileType.Hand, player, position);
            BookLibraryUtility.RefreshHandCardVisual(duplicate);
            return duplicate;
        }

        if (!BookLibraryUtility.IsBookLibraryPile(pile.Type))
        {
            return null;
        }

        await CardPileCmd.AddGeneratedCardToCombat(duplicate, BookLibraryUtility.PileType, player, position);
        BookLibraryUtility.RefreshCardVisual(duplicate);
        return duplicate;
    }
    #endregion

    #region Remove
    public static async Task SwapHandAndLibrary(Player player)
    {
        var libraryPile = BookLibraryUtility.TryGetLibraryPile(player);
        if (libraryPile is null)
        {
            return;
        }

        var handCards = PileType.Hand.GetPile(player).Cards.ToList();
        var libraryCards = libraryPile.Cards.ToList();

        foreach (var card in handCards)
        {
            await CardPileCmd.Add(card, BookLibraryUtility.PileType);
            BookLibraryUtility.RefreshCardVisual(card);
        }

        foreach (var card in libraryCards)
        {
            await CardPileCmd.Add(card, PileType.Hand);
            BookLibraryUtility.RefreshHandCardVisual(card);
        }
    }

    public static async Task TryTriggerKnowledgeOverloadIfThresholdReached(
        PlayerChoiceContext? choiceContext,
        Player player,
        AbstractModel? source = null)
    {
        if (choiceContext is null)
        {
            return;
        }

        var libraryPile = BookLibraryUtility.TryGetLibraryPile(player);
        if (libraryPile is null || libraryPile.Cards.Count < 9)
        {
            return;
        }

        if (player.GetRelic<CognitionVesselRelic>() is { } cognitionVesselRelic)
        {
            cognitionVesselRelic.Flash();
            await TriggerKnowledgeOverload(choiceContext, player, source ?? cognitionVesselRelic);
            return;
        }

        if (player.GetRelic<KnowledgeHostRelic>() is { } knowledgeHostRelic)
        {
            knowledgeHostRelic.Flash();
            await TriggerKnowledgeOverload(choiceContext, player, source ?? knowledgeHostRelic, true);
            return;
        }

        if (player.Creature.GetPower<TelepathyPower>() is { } telepathyPower)
        {
            await TriggerKnowledgeOverload(choiceContext, player, source ?? telepathyPower);
        }
    }

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

    public static async Task DismissLibraryAtTurnEnd(PlayerChoiceContext choiceContext, Player player)
    {
        if (!BookLibraryUtility.PlayerHasBookLibraryAccess(player))
        {
            return;
        }

        var libraryPile = BookLibraryUtility.PileType.GetPile(player);
        if (libraryPile.Cards.Count == 0)
        {
            return;
        }

        await CardPileCmd.RemoveFromCombat(libraryPile.Cards.ToList(), skipVisuals: false);
    }

    public static async Task TriggerKnowledgeOverload(
        PlayerChoiceContext choiceContext,
        Player player,
        AbstractModel? source = null,
        bool IsUpgraded = false)
    {
        var libraryPile = BookLibraryUtility.TryGetLibraryPile(player);
        if (libraryPile is null || libraryPile.Cards.Count == 0)
        {
            return;
        }

        NBookLibraryPile.Instance?.ForceShowCards();
        try
        {
            var libraryCountBefore = libraryPile.Cards.Count;
            var chooseCount = 0;
            await KnowledgeDemonHook.AfterKnowledgeOverloadTriggered(
                choiceContext,
                player,
                source);

            if (player.Character is KnowledgeDemon)
            {
                await CreatureCmd.TriggerAnim(
                    player.Creature,
                    "superAttack",
                    0f);
            }
            TalkCmd.Play(ChooseStartLine, player.Creature, VfxColor.Gold, VfxDuration.Standard);
            await Cmd.CustomScaledWait(0.5f, 1f);
            int chooseOfferCount = IsUpgraded ? 2 : 3;
            while (libraryPile.Cards.Count >= chooseOfferCount && !CombatManager.Instance.IsOverOrEnding)
            {
                await ChooseFromLibraryAndAutoPlay(choiceContext, player, source as CardModel, chooseOfferCount: chooseOfferCount);
                chooseCount++;
            }

            PlayChooseDonePresentation(player);
            KnowledgeDemonTelemetryEvents.CaptureKnowledgeOverloadResolved(
                player,
                source as CardModel,
                libraryCountBefore,
                libraryPile.Cards.Count,
                chooseCount,
                chooseOfferCount);
        }
        finally
        {
            NBookLibraryPile.Instance?.ReleaseForcedShowCards();
        }
    }
    #endregion
}
