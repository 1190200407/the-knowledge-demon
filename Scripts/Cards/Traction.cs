using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class Traction : KnowledgeDemonCardModel, IKnowledgeDemonEventListener
{
    private const int EnergyCostValue = 0;
    private const CardType TypeValue = CardType.Skill;
    private const CardRarity RarityValue = CardRarity.Uncommon;
    private const TargetType TargetTypeValue = TargetType.Self;
    private const bool ShouldShowInCardLibraryValue = true;
    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [KnowledgeDemonKeywordHoverTips.FromMaterializeKeyword(), HoverTipFactory.FromCard<MindAscension>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new BlockVar(6m, ValueProp.Move),
        ];

    public Traction()
        : base(EnergyCostValue, TypeValue, RarityValue, TargetTypeValue, ShouldShowInCardLibraryValue)
    {
    }

    public async Task AfterMaterializedFromLibrary(
        PlayerChoiceContext choiceContext,
        Player player,
        IReadOnlyList<CardModel> materialized)
    {
        if (!materialized.Contains(this))
        {
            return;
        }

        var mindAscension = player.RunState.CreateCard<MindAscension>(player);
        await CardPileCmd.AddGeneratedCardToCombat(mindAscension, PileType.Hand, player);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}
