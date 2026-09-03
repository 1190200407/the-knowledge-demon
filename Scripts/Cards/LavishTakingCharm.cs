using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class LavishTakingCharm : KnowledgeDemonCardModel, IKnowledgeDemonEventListener
{
    private const int energyCost = 1;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Common;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<VulnerablePower>(),
        KnowledgeDemonKeywordHoverTips.FromRecord(),
        HoverTipFactory.Static(StaticHoverTip.Transform),
        HoverTipFactory.FromCard<RefusalCharm>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(7m, ValueProp.Move),
        new PowerVar<VulnerablePower>(1m),
    ];

    public LavishTakingCharm()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    public Task<CardModel> ModifyRecordCard(Player player, CardModel sourceCard, CardModel recordTemplate)
    {
        if (!ReferenceEquals(sourceCard, this) || sourceCard.CardScope is null)
        {
            return Task.FromResult(recordTemplate);
        }

        var refusal = sourceCard.CardScope.CreateCard<RefusalCharm>(player);
        if (sourceCard.IsUpgraded && !refusal.IsUpgraded)
        {
            CardCmd.Upgrade(refusal, CardPreviewStyle.None);
        }

        return Task.FromResult((CardModel)refusal);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(choiceContext);

        await PowerCmd.Apply<VulnerablePower>(
            choiceContext,
            cardPlay.Target,
            DynamicVars.Vulnerable.BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars.Vulnerable.UpgradeValueBy(1m);
    }
}
