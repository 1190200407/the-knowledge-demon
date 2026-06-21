using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class BloodSacrificeOption : KnowledgeDemonCardModel, IItIsDoneRewardOption
{
    private const string HealVarName = "Heal";

    private const int energyCost = -1;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Token;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = false;

    public override int MaxUpgradeLevel => 1;

    public override bool CanBeGeneratedInCombat => false;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar(HealVarName, 5m)];

    public BloodSacrificeOption()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override void OnUpgrade()
    {
        DynamicVars[HealVarName].UpgradeValueBy(3m);
    }

    public async Task OnChosen()
    {
        await CreatureCmd.Heal(Owner.Creature, DynamicVars[HealVarName].IntValue);
    }
}
