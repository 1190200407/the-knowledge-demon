using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class DejaVuPower : KnowledgeDemonPowerModel, IKnowledgeDemonEventListener
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<string> RegisteredKeywordIds =>
        [KnowledgeDemonKeyword.Materialize];

    public Task AfterMaterializedFromLibrary(
        MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceContext choiceContext,
        Player player,
        IReadOnlyList<CardModel> materialized)
    {
        _ = choiceContext;

        if (Owner.Player != player)
        {
            return Task.CompletedTask;
        }

        foreach (var card in materialized)
        {
            if (card.Owner != player)
            {
                continue;
            }

            GrantSly(card);
        }

        return Task.CompletedTask;
    }

    private static void GrantSly(CardModel card)
    {
        if (!card.Keywords.Contains(CardKeyword.Sly))
        {
            card.AddKeyword(CardKeyword.Sly);
        }
    }
}
