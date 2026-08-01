using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class Alertness : KnowledgeDemonCardModel
{
    private const int EnergyCostValue = -1;
    private const CardType TypeValue = CardType.Status;
    private const CardRarity RarityValue = CardRarity.Common;
    private const TargetType TargetTypeValue = TargetType.None;
    private const bool ShouldShowInCardLibraryValue = true;

    public override int MaxUpgradeLevel => 0;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<StrengthPower>(2m),
    ];

    public Alertness()
        : base(EnergyCostValue, TypeValue, RarityValue, TargetTypeValue, ShouldShowInCardLibraryValue)
    {
    }

    public override async Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        AbstractModel? clonedBy)
    {
        _ = oldPileType;
        _ = clonedBy;

        if (card != this || Owner is null || CombatState is null)
        {
            return;
        }

        if (Pile is not { } enteredPile
            || (enteredPile.Type != PileType.Hand && !BookLibraryUtility.IsBookLibraryPile(enteredPile.Type)))
        {
            return;
        }

        IReadOnlyList<Creature> targets = CombatState.HittableEnemies;
        if (targets.Count == 0)
        {
            return;
        }

        Creature? target = Owner.RunState.Rng.CombatTargets.NextItem(targets);
        if (target is null)
        {
            return;
        }

        await PowerCmd.Apply<AlertnessStrengthDownPower>(
            new ThrowingPlayerChoiceContext(),
            target,
            DynamicVars["StrengthPower"].BaseValue,
            Owner.Creature,
            this);
    }
}
