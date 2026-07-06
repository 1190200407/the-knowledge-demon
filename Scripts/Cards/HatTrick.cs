using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class HatTrick : KnowledgeDemonCardModel
{
    private const int EnergyCostValue = 1;
    private const CardType TypeValue = CardType.Skill;
    private const CardRarity RarityValue = CardRarity.Rare;
    private const TargetType TargetTypeValue = TargetType.Self;
    private const bool ShouldShowInCardLibraryValue = true;
    private const string PowerVarName = "Power";

    public override IEnumerable<CardKeyword> CanonicalKeywords => 
    [
        ModKeywordRegistry.GetCardKeyword(KnowledgeDemonKeyword.Unique)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        KnowledgeDemonKeywordHoverTips.FromMaterialize(DynamicVars),
        KnowledgeDemonKeywordHoverTips.FromRecord(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<HatTrickPower>(PowerVarName, 3m),
        new MaterializeVar(1),
    ];

    public HatTrick()
        : base(EnergyCostValue, TypeValue, RarityValue, TargetTypeValue, ShouldShowInCardLibraryValue)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<HatTrickPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars[PowerVarName].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Sly);
    }
}
