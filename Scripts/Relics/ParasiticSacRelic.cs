using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterRelic(typeof(KnowledgeDemonRelicPool))]
public sealed class ParasiticSacRelic : KnowledgeDemonRelicModel
{
    public override bool ShowCounter => true;

    public override int DisplayAmount => base.IsMutable ? GetStatusCount() : 0;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>(),
    ];

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override async Task BeforeCombatStart()
    {
        var statusCount = GetStatusCount();
        if (statusCount <= 0)
        {
            return;
        }

        Flash();
        var context = new ThrowingPlayerChoiceContext();
        await PowerCmd.Apply<StrengthPower>(context, Owner.Creature, statusCount, Owner.Creature, null);
        await PowerCmd.Apply<DexterityPower>(context, Owner.Creature, statusCount, Owner.Creature, null);
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (card.Owner != Owner)
        {
            return Task.CompletedTask;
        }

        var newPileType = card.Pile?.Type;
        if (oldPileType == PileType.Deck || newPileType == PileType.Deck)
        {
            InvokeDisplayAmountChanged();
        }

        return Task.CompletedTask;
    }

    private int GetStatusCount() =>
        PileType.Deck.GetPile(Owner).Cards.Count(static card => card.Type == CardType.Status);
}
