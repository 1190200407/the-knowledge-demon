using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class AberrantPower : KnowledgeDemonPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Retain)];

    public override async Task BeforeSideTurnEndEarly(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player
            || !participants.Contains(Owner)
            || Owner.Player is not { } player)
        {
            return;
        }

        var candidates = GetStatusCandidates(player);
        if (candidates.Count == 0)
        {
            return;
        }

        for (var i = 0; i < Amount; i++)
        {
            var chosenCard = player.RunState.Rng.CombatCardGeneration.NextItem(candidates)!;
            Flash();
            var card = await BookLibraryCmd.DuplicateCardToCurrentPile(player, chosenCard);
            card?.AddKeyword(CardKeyword.Retain);
        }
    }

    private static List<CardModel> GetStatusCandidates(Player player) =>
        PileType.Hand.GetPile(player).Cards
            .Concat(BookLibraryUtility.TryGetLibraryPile(player)?.Cards ?? [])
            .Where(static card => card.Type == CardType.Status)
            .ToList();
}
