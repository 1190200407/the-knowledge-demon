using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class ItIsDone : KnowledgeDemonCardModel
{
    private const int energyCost = 2;
    private const CardType type = CardType.Power;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Eternal];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        ItIsDoneOptionHoverTips.All(IsUpgraded);

    public ItIsDone()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        if (IsUpgraded)
        {
            var basePower = Owner.Creature.GetPower<ItIsDonePower>();
            if (basePower is not null)
            {
                await PowerCmd.Remove(basePower);
            }

            await PowerCmd.Apply<ItIsDoneUpgradedPower>(
                choiceContext,
                Owner.Creature,
                1m,
                Owner.Creature,
                this);
        }
        else if (Owner.Creature.GetPower<ItIsDoneUpgradedPower>() is null)
        {
            await PowerCmd.Apply<ItIsDonePower>(
                choiceContext,
                Owner.Creature,
                1m,
                Owner.Creature,
                this);
        }
    }
}
