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
public sealed class EnvironmentalTolerance : KnowledgeDemonCardModel
{
    private const int EnergyCostValue = 1;
    private const CardType TypeValue = CardType.Skill;
    private const CardRarity RarityValue = CardRarity.Uncommon;
    private const TargetType TargetTypeValue = TargetType.Self;
    private const bool ShouldShowInCardLibraryValue = true;

    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.Transform)];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(5m, ValueProp.Move)];

    public EnvironmentalTolerance()
        : base(EnergyCostValue, TypeValue, RarityValue, TargetTypeValue, ShouldShowInCardLibraryValue)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        var statusCards = PileType.Hand.GetPile(Owner).Cards
            .Where(card => card.Owner == Owner && card.Type == CardType.Status)
            .ToList();

        foreach (var statusCard in statusCards)
        {
            if (statusCard.Owner != Owner
                || statusCard.Pile?.Type != PileType.Hand
                || statusCard.Type != CardType.Status
                || !statusCard.IsTransformable)
            {
                continue;
            }

            var candidates = TransformOptionUtility
                .GetKnowledgeDemonStatusTransformCandidates(Owner, statusCard, isInCombat: true)
                .ToArray();
            if (candidates.Length == 0)
            {
                continue;
            }

            var template = Owner.PlayerRng.Transformations.NextItem(candidates);
            if (template is null)
            {
                continue;
            }

            var replacement = statusCard.CardScope?.CreateCard(template, Owner)
                ?? Owner.RunState.CreateCard(template, Owner);
            await BookLibraryUtility.TransformCard(statusCard, replacement);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}
