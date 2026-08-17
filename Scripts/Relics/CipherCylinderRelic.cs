using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterRelic(typeof(KnowledgeDemonRelicPool))]
public sealed class CipherCylinderRelic : KnowledgeDemonRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [KnowledgeDemonKeywordHoverTips.FromUnique()];

    public override async Task BeforeCombatStart()
    {
        var combatState = Owner.Creature.CombatState;
        if (combatState is null)
        {
            return;
        }

        var candidates = ModelDb.AllCards
            .Where(static card =>
                card.CanBeGeneratedInCombat
                && card.Rarity != CardRarity.Ancient
                && card.Rarity != CardRarity.Event
                && card.Rarity != CardRarity.Token
                && KnowledgeDemonUniqueUtility.IsUnique(card))
            .ToArray();

        if (candidates.Length == 0)
        {
            return;
        }

        var canonical = Owner.RunState.Rng.CombatCardGeneration.NextItem(candidates);
        if (canonical is null)
        {
            return;
        }

        Flash();
        var generated = combatState.CreateCard(canonical, Owner);
        await CardPileCmd.AddGeneratedCardToCombat(generated, PileType.Hand, Owner);
    }
}
