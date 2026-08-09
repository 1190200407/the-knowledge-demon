using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class MentalStrike : KnowledgeDemonCardModel
{
    private const int energyCost = 1;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Common;
    private const TargetType targetType = TargetType.RandomEnemy;
    private const bool shouldShowInCardLibrary = true;

    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(2),
        new DamageVar(4m, ValueProp.Move),
    ];

    public MentalStrike()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var discardedCards = await BookLibraryCmd.DiscardFromLibraryAndHand(
            choiceContext,
            Owner,
            DynamicVars.Cards.IntValue,
            SelectionScreenPrompt,
            this);

        if (discardedCards.Count == 0)
        {
            return;
        }

        await KnowledgeDemon.WithKnowledgeDemonAttackAnim(
            DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .WithHitCount(discardedCards.Count)
                .FromCard(this)
                .TargetingRandomOpponents(CombatState!),
            Owner.Character,
            onlyPlayAnimOnce: true)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}
