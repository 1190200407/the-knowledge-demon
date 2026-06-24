using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class RetainHandAndLibraryPower : KnowledgeDemonPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldFlush(Player player) => player != Owner.Player;

    public override Task BeforeFlush(PlayerChoiceContext choiceContext, Player player)
    {
        _ = choiceContext;

        if (player != Owner.Player)
        {
            return Task.CompletedTask;
        }

        var libraryPile = BookLibraryUtility.TryGetLibraryPile(player);
        if (libraryPile is null)
        {
            return Task.CompletedTask;
        }

        foreach (var card in libraryPile.Cards)
        {
            card.GiveSingleTurnRetain();
        }

        return Task.CompletedTask;
    }

    public override async Task AfterFlush(
        PlayerChoiceContext choiceContext,
        Player player,
        IReadOnlyCollection<CardModel> flushedCards,
        IReadOnlyCollection<CardModel> retainedCards)
    {
        _ = choiceContext;
        _ = flushedCards;
        _ = retainedCards;

        if (player == Owner.Player)
        {
            await PowerCmd.Remove(this);
        }
    }
}
