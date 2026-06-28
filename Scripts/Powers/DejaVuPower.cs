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

    public Task<CardModel> ModifyMaterializeCard(Player player, CardModel sourceCard, CardModel materializedCard)
    {
        _ = sourceCard;

        if (Owner.Player != player)
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
