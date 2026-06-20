using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class RefusalCharm : KnowledgeDemonCardModel, IKnowledgeDemonEventListener
{
    private const int energyCost = 1;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Common;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.Transform),
        HoverTipFactory.FromCard<LavishTakingCharm>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(6m, ValueProp.Move)];

    public RefusalCharm()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    public Task<CardModel> ModifyRecordCard(Player player, CardModel sourceCard, CardModel recordTemplate)
    {
        if (!ReferenceEquals(sourceCard, this) || sourceCard.CardScope is null)
        {
            return Task.FromResult(recordTemplate);
        }

        var lavish = sourceCard.CardScope.CreateCard<LavishTakingCharm>(player);
        if (sourceCard.IsUpgraded && !lavish.IsUpgraded)
        {
            CardCmd.Upgrade(lavish, CardPreviewStyle.None);
        }

        return Task.FromResult((CardModel)lavish);
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
