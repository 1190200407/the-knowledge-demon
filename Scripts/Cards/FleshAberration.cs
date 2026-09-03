using System;
using System.Collections.Generic;
using System.Linq;
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
public sealed class FleshAberration : KnowledgeDemonCardModel
{
    private const int EnergyCostValue = 1;
    private const CardType TypeValue = CardType.Attack;
    private const CardRarity RarityValue = CardRarity.Uncommon;
    private const TargetType TargetTypeValue = TargetType.AnyEnemy;
    private const bool ShouldShowInCardLibraryValue = true;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<FleshAberrationStrengthDownPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Move),
    ];

    public FleshAberration()
        : base(EnergyCostValue, TypeValue, RarityValue, TargetTypeValue, ShouldShowInCardLibraryValue)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(choiceContext);

        var statusCount = CountStatusCardsInHandAndLibrary();
        if (statusCount <= 0)
        {
            return;
        }

        await PowerCmd.Apply<FleshAberrationStrengthDownPower>(
            choiceContext,
            cardPlay.Target,
            statusCount,
            Owner.Creature,
            this);
    }

    private int CountStatusCardsInHandAndLibrary()
    {
        var handCount = PileType.Hand.GetPile(Owner).Cards.Count(card => card.Type == CardType.Status);
        if (!IsUpgraded)
        {
            return handCount;
        }

        var libraryCount = BookLibraryUtility.TryGetLibraryPile(Owner)?.Cards.Count(card => card.Type == CardType.Status) ?? 0;
        return handCount + libraryCount;
    }
}
