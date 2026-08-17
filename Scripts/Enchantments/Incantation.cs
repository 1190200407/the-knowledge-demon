using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterEnchantment]
public sealed class Incantation : KnowledgeDemonEnchantmentModel
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(CardKeyword.Sly)];

    public override bool CanEnchant(CardModel card)
    {
        if (!base.CanEnchant(card))
        {
            return false;
        }

        return !card.Keywords.Contains(CardKeyword.Sly);
    }

    protected override void OnEnchant()
    {
        if (!Card.Keywords.Contains(CardKeyword.Sly))
        {
            Card.AddKeyword(CardKeyword.Sly);
        }
    }
}
