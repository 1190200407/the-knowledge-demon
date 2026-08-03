using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class AncientPulsePower : KnowledgeDemonPowerModel, IKnowledgeDemonEventListener
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public Task<CardModel> ModifyRecordCardLate(Player player, CardModel sourceCard, CardModel recordTemplate)
    {
        if (Owner.Player != player)
        {
            return Task.FromResult(recordTemplate);
        }

        if (recordTemplate.Type == CardType.Status && recordTemplate.Pool is KnowledgeDemonCardPool)
        {
            return Task.FromResult(recordTemplate);
        }

        var randomStatusTemplate = KnowledgeDemonCardCmd.GetRandomKnowledgeDemonStatusCardTemplate(player);
        if (randomStatusTemplate is null)
        {
            return Task.FromResult(recordTemplate);
        }

        var mutableStatusCard = sourceCard.CardScope?.CreateCard(randomStatusTemplate, player)
            ?? player.RunState.CreateCard(randomStatusTemplate, player);

        Flash();
        return Task.FromResult(mutableStatusCard);
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        _ = choiceContext;
        _ = participants;

        if (side == CombatSide.Player)
        {
            await PowerCmd.Remove(this);
        }
    }
}
