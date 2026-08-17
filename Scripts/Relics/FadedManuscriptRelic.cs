using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterRelic(typeof(KnowledgeDemonRelicPool))]
public sealed class FadedManuscriptRelic : KnowledgeDemonRelicModel, ITransformOptionCandidateProvider
{
    public override RelicRarity Rarity => RelicRarity.Shop;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.Transform)];

    public IEnumerable<CardModel> GetAdditionalTransformCandidates(
        Player player,
        CardModel original,
        bool isInCombat)
    {
        _ = original;
        _ = isInCombat;

        return ModelDb.CardPool<ColorlessCardPool>().GetUnlockedCards(
            player.UnlockState,
            player.RunState.CardMultiplayerConstraint);
    }
}
