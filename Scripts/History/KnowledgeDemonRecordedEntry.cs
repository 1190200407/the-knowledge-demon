using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace ComicChess.KnowledgeDemon;

public sealed class KnowledgeDemonRecordedEntry : CombatHistoryEntry
{
    public Player Player { get; }

    public CardModel? SourceCard { get; }

    public ModelId RecordedCardId { get; }

    public int Count { get; }

    public override string Description =>
        $"{Player.Character.Id.Entry} recorded {RecordedCardId.Entry} x{Count}";

    public KnowledgeDemonRecordedEntry(
        Player player,
        CardModel? sourceCard,
        ModelId recordedCardId,
        int count,
        int roundNumber,
        CombatSide currentSide,
        CombatHistory history,
        IEnumerable<Player> players)
        : base(player.Creature, roundNumber, currentSide, history, players)
    {
        Player = player;
        SourceCard = sourceCard;
        RecordedCardId = recordedCardId;
        Count = count;
    }
}
