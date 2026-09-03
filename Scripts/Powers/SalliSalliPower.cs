using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class SalliSalliPower : KnowledgeDemonPowerModel
{
    private CardModel? _autoPlayedLibraryCard;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override CardLocation ModifyCardPlayResultLocation(
        CardModel card,
        bool isAutoPlay,
        ResourceInfo resources,
        CardLocation cardLocation)
    {
        _ = resources;

        if (isAutoPlay && ReferenceEquals(card, _autoPlayedLibraryCard))
        {
            return new CardLocation(cardLocation.player, PileType.None, cardLocation.position);
        }

        return cardLocation;
    }

    public override Task AfterModifyingCardPlayResultLocation(
        CardModel card,
        CardLocation cardLocation)
    {
        _ = card;
        _ = cardLocation;

        _autoPlayedLibraryCard = null;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (Owner.Player is not { } player
            || cardPlay.IsAutoPlay
            || cardPlay.Card.Owner != player)
        {
            return;
        }

        var libraryPile = BookLibraryUtility.TryGetLibraryPile(player);
        if (libraryPile is null)
        {
            return;
        }

        var matchingCard = libraryPile.Cards
            .FirstOrDefault(card => card.Id.Entry == cardPlay.Card.Id.Entry);
        if (matchingCard is null)
        {
            return;
        }

        Flash();

        var combatState = player.Creature.CombatState;
        if (combatState is null || matchingCard.Pile is not { } pile)
        {
            return;
        }

        var oldPileType = pile.Type;
        matchingCard.RemoveFromCurrentPile(silent: false);
        BookLibraryUtility.ResetCardTint(matchingCard);
        await Cmd.CustomScaledWait(0.2f, 0.5f);
        await Hook.AfterCardChangedPiles(player.RunState, combatState, matchingCard, oldPileType, null);
        _autoPlayedLibraryCard = matchingCard;
        try
        {
            await CardCmd.AutoPlay(choiceContext, matchingCard, null);
        }
        finally
        {
            _autoPlayedLibraryCard = null;
        }
    }
}
