using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class HatTrickPower : KnowledgeDemonPowerModel, IKnowledgeDemonEventListener
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<string> RegisteredKeywordIds =>
        [KnowledgeDemonKeyword.Record, KnowledgeDemonKeyword.Materialize];

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        _ = participants;
        _ = combatState;

        if (side != CombatSide.Player || Owner.Player is not { } player)
        {
            return;
        }

        var combatStateCards = player.PlayerCombatState?.AllCards
            .Where(card => card.Owner == player && card.Type == CardType.Power && card.Pile is not { Type: PileType.Exhaust })
            .GroupBy(card => card.Id)
            .Select(group => group.First())
            .ToList() ?? [];

        if (combatStateCards.Count == 0)
        {
            return;
        }

        var selected = combatStateCards
            .StableShuffle(player.RunState.Rng.Shuffle)
            .Take(Math.Min((int)Amount, combatStateCards.Count))
            .ToList();

        if (selected.Count == 0)
        {
            return;
        }

        Flash();
        foreach (var card in selected)
        {
            await BookLibraryCmd.RecordToLibrary(choiceContext, player, card, 1);
        }

        await BookLibraryCmd.MaterializeFromLibraryToHand(
            choiceContext,
            player,
            1,
            SelectionScreenPrompt,
            this);
    }

    public Task<CardModel> ModifyMaterializeCard(Player player, CardModel sourceCard, CardModel materializedCard)
    {
        _ = sourceCard;

        if (Owner.Player != player || materializedCard.Type != CardType.Power)
        {
            return Task.FromResult(materializedCard);
        }

        if (!materializedCard.Keywords.Contains(CardKeyword.Sly))
        {
            materializedCard.AddKeyword(CardKeyword.Sly);
        }

        return Task.FromResult(materializedCard);
    }
}
