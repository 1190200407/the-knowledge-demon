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
public sealed class HatTrickPower : KnowledgeDemonPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override LocString Title => HatTrickPowerShared.Title;

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (Owner.Player != player)
        {
            return;
        }

        Flash();
        await HatTrickPowerShared.RecordAndMaterializeAsync(
            choiceContext,
            player,
            upgradeRecordedCards: false,
            SelectionScreenPrompt,
            this);
    }
}

[RegisterPower]
public sealed class HatTrickUpgradedPower : KnowledgeDemonPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override LocString Title => HatTrickPowerShared.Title;

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (Owner.Player != player)
        {
            return;
        }

        Flash();
        await HatTrickPowerShared.RecordAndMaterializeAsync(
            choiceContext,
            player,
            upgradeRecordedCards: true,
            SelectionScreenPrompt,
            this);
    }
}

internal static class HatTrickPowerShared
{
    internal static readonly LocString Title = new("powers", "KNOWLEDGE_DEMON_POWER_HAT_TRICK_POWER.title");

    internal static async Task RecordAndMaterializeAsync(
        PlayerChoiceContext choiceContext,
        Player player,
        bool upgradeRecordedCards,
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
            .Take(Math.Min(3, candidates.Count))
            .ToList();

        if (selected.Count == 0)
        {
            return;
        }

        foreach (var template in selected)
        {
            var record = player.RunState.CreateCard(template, player);
            if (upgradeRecordedCards && !record.IsUpgraded)
            {
                CardCmd.Upgrade(record, CardPreviewStyle.None);
            }

            await BookLibraryCmd.RecordToLibrary(choiceContext, player, record, 1);
        }

        await Cmd.CustomScaledWait(0.25f, 0.45f);

        await BookLibraryCmd.MaterializeFromLibraryToHand(
            choiceContext,
            player,
            1,
            selectionPrompt,
            source);
    }
}
