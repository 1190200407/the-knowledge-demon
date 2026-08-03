using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class CombatExperience : KnowledgeDemonCardModel
{
    private const int energyCost = 3;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Uncommon;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(7m),
        new ExtraDamageVar(4m),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(CalculateAttackCardCount),
    ];

    public CombatExperience()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.ExtraDamage.UpgradeValueBy(1m);
    }

    private static decimal CalculateAttackCardCount(CardModel card, Creature? target)
    {
        _ = target;
        if (card.Owner is null)
        {
            return 0;
        }

        var history = CombatManager.Instance?.History;
        if (history is null)
        {
            return 0;
        }

        return history.CardPlaysFinished.Count(entry =>
            entry.CardPlay.Card.Owner == card.Owner
            && entry.CardPlay.Card.Type == CardType.Attack);
    }
}
