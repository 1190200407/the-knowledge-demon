using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class Dualism : KnowledgeDemonCardModel
{
    private const int energyCost = 2;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Uncommon;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;
    private const string DamageVarName = "Damage";
    private const string BonusVarName = "Amount";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        ModCardVars.ComputedDamage(DamageVarName, 5m, CalculateDamage, ValueProp.Move),
        new DynamicVar(BonusVarName, 4m),
    ];

    public Dualism()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars[BonusVarName].UpgradeValueBy(1m);
    }

    private decimal CalculateDamage(CardModel? card, Creature? target)
    {
        _ = target;
        if (card?.Owner is not { } player)
        {
            return 0m;
        }

        var bonusPerUnique = card.DynamicVars[BonusVarName].BaseValue;
        var uniqueCount = PileType.Deck.GetPile(player).Cards.Count(KnowledgeDemonUniqueUtility.IsUnique);
        return card.DynamicVars.Damage.BaseValue + uniqueCount * bonusPerUnique;
    }
}
