using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class EnlightenmentAttained : KnowledgeDemonCardModel
{
    private const string HealVarName = "Heal";
    private const string GoldVarName = "Gold";

    private const int energyCost = 2;
    private const CardType type = CardType.Power;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar(HealVarName, 7m),
        new IntVar(GoldVarName, 15m),
    ];

    public EnlightenmentAttained()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        if (IsUpgraded)
        {
            var basePower = Owner.Creature.GetPower<EnlightenmentAttainedPower>();
            if (basePower is not null)
            {
                await PowerCmd.Remove(basePower);
            }

            await PowerCmd.Apply<EnlightenmentAttainedUpgradedPower>(
                choiceContext,
                Owner.Creature,
                1m,
                Owner.Creature,
                this);
        }
        else if (Owner.Creature.GetPower<EnlightenmentAttainedUpgradedPower>() is null)
        {
            await PowerCmd.Apply<EnlightenmentAttainedPower>(
                choiceContext,
                Owner.Creature,
                1m,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars[HealVarName].UpgradeValueBy(3m);
        DynamicVars[GoldVarName].UpgradeValueBy(5m);
    }
}
