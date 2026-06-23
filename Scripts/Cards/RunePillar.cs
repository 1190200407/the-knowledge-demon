using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class RunePillar : KnowledgeDemonCardModel
{
    private const int energyCost = -1;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Uncommon;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;

    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromKeyword(CardKeyword.Sly)];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(6m, ValueProp.Move)];

    public RunePillar()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        var hitCount = ResolveEnergyXValue();
        if (hitCount > 0)
        {
            await KnowledgeDemon.WithKnowledgeDemonAttackAnim(
                DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .WithHitCount(hitCount)
                    .FromCard(this)
                    .Targeting(cardPlay.Target),
                Owner.Character,
                onlyPlayAnimOnce: true)
                .WithHitFx("vfx/vfx_heavy_blunt")
                .Execute(choiceContext);
        }

        if (!Keywords.Contains(CardKeyword.Sly))
        {
            CardCmd.ApplyKeyword(this, CardKeyword.Sly);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
