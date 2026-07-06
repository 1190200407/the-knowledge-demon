using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace ComicChess.KnowledgeDemon;

public sealed class KnowledgeDemonChooseEntry : CombatHistoryEntry
{
    public Player Player { get; }

    public CardModel? SourceCard { get; }

    public CardModel ChosenCard { get; }

    public override string Description =>
        $"{Player.Character.Id.Entry} chose {ChosenCard.Id.Entry} from the library";

    public KnowledgeDemonChooseEntry(
        Player player,
        CardModel? sourceCard,
        CardModel chosenCard,
        int roundNumber,
        CombatSide currentSide,
        CombatHistory history,
        IEnumerable<Player> players)
        : base(player.Creature, roundNumber, currentSide, history, players)
    {
        Player = player;
        SourceCard = sourceCard;
        ChosenCard = chosenCard;
    }
}
