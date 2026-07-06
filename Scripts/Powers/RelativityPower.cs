using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class RelativityPower : KnowledgeDemonPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => RelativityPowerShared.Title;

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (Owner.Player != player)
        {
            return;
        }

        Flash();
        await RelativityPowerShared.RecordAndMaterializeAsync(
            choiceContext,
            player,
            (int)System.Math.Max(1m, Amount),
            SelectionScreenPrompt,
            this);
    }
}

[RegisterPower]
public sealed class RelativityUpgradedPower : KnowledgeDemonPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Title => RelativityPowerShared.Title;

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (Owner.Player != player)
        {
            return;
        }

        Flash();
        await RelativityPowerShared.RecordAndMaterializeAsync(
            choiceContext,
            player,
            (int)System.Math.Max(1m, Amount),
            SelectionScreenPrompt,
            this);
    }
}

internal static class RelativityPowerShared
{
    internal static readonly LocString Title = new("powers", "KNOWLEDGE_DEMON_POWER_RELATIVITY_POWER.title");

    internal static async Task RecordCardsWithDelayAsync(
        PlayerChoiceContext choiceContext,
        Player player,
        IReadOnlyList<CardModel> cards)
    {
        foreach (var card in cards)
        {
            await Cmd.CustomScaledWait(0.1f, 0.1f);
            await BookLibraryCmd.RecordToLibrary(choiceContext, player, card, 1);
        }
    }

    internal static async Task RecordAndMaterializeAsync(
        PlayerChoiceContext choiceContext,
        Player player,
        int recordCount,
        LocString selectionPrompt,
        PowerModel? source = null)
    {
        var candidates = player.Character.CardPool
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Where(card => card.Type == CardType.Power)
            .Where(card => card.CanBeGeneratedInCombat)
            .Where(card => card.Rarity != CardRarity.Basic)
            .Where(card => card.Rarity != CardRarity.Ancient)
            .GroupBy(card => card.Id)
            .Select(group => group.First())
            .ToList();

        if (candidates.Count == 0)
        {
            return;
        }

        var selected = candidates
            .StableShuffle(player.RunState.Rng.Shuffle)
            .Take(System.Math.Max(1, recordCount))
            .ToList();

        if (selected.Count == 0)
        {
            return;
        }

        var records = selected
            .Select(template => player.RunState.CreateCard(template, player))
            .ToList();

        await RecordCardsWithDelayAsync(choiceContext, player, records);

        await Cmd.CustomScaledWait(0.5f, 0.5f);

        await BookLibraryCmd.MaterializeFromLibraryToHand(
            choiceContext,
            player,
            1,
            selectionPrompt,
            source);
    }
}
