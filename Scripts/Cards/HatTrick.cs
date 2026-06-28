using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class HatTrick : KnowledgeDemonCardModel
{
    private const int EnergyCostValue = 3;
    private const CardType TypeValue = CardType.Power;
    private const CardRarity RarityValue = CardRarity.Rare;
    private const TargetType TargetTypeValue = TargetType.Self;
    private const bool ShouldShowInCardLibraryValue = true;
    private const string RecordVarName = "Record";
    private const string MaterializeVarName = "Materialize";

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        KnowledgeDemonKeywordHoverTips.FromRecord(),
        KnowledgeDemonKeywordHoverTips.FromMaterialize(DynamicVars),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<HatTrickPower>(RecordVarName, 3m),
        new MaterializeVar(1),
    ];

    public HatTrick()
        : base(EnergyCostValue, TypeValue, RarityValue, TargetTypeValue, ShouldShowInCardLibraryValue)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<HatTrickPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars[RecordVarName].BaseValue,
            Owner.Creature,
            this);

        await BookLibraryCmd.MaterializeFromLibraryToHand(
            choiceContext,
            Owner,
            DynamicVars[MaterializeVarName].IntValue,
            SelectionScreenPrompt,
            this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars[RecordVarName].UpgradeValueBy(1m);
    }
}
