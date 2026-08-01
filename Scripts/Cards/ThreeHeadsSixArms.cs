using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Enchantments;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class ThreeHeadsSixArms : KnowledgeDemonCardModel
{
    private const int EnergyCostValue = 2;
    private const CardType TypeValue = CardType.Power;
    private const CardRarity RarityValue = CardRarity.Uncommon;
    private const TargetType TargetTypeValue = TargetType.Self;
    private const bool ShouldShowInCardLibraryValue = true;
    private const decimal EnchantmentAmount = 2m;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        IsUpgraded
            ? HoverTipFactory.FromEnchantment<Adroit>(2).Concat(HoverTipFactory.FromEnchantment<Momentum>(2))
            : HoverTipFactory.FromEnchantment<Nimble>(2).Concat(HoverTipFactory.FromEnchantment<Sharp>(2));

    public ThreeHeadsSixArms()
        : base(EnergyCostValue, TypeValue, RarityValue, TargetTypeValue, ShouldShowInCardLibraryValue)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;

        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        if (IsUpgraded)
        {
            await PowerCmd.Apply<ThreeHeadsSixArmsUpgradedPower>(
                choiceContext,
                Owner.Creature,
                EnchantmentAmount,
                Owner.Creature,
                this);
            return;
        }

        await PowerCmd.Apply<ThreeHeadsSixArmsPower>(
            choiceContext,
            Owner.Creature,
            EnchantmentAmount,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
    }
}
