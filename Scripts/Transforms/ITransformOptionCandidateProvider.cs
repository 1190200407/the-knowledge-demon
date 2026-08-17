using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace ComicChess.KnowledgeDemon;

internal interface ITransformOptionCandidateProvider
{
    IEnumerable<CardModel> GetAdditionalTransformCandidates(
        Player player,
        CardModel original,
        bool isInCombat);
}
