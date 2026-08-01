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
public sealed class Manifestation : KnowledgeDemonCardModel
{
    private const int EnergyCostValue = 0;
    private const CardType TypeValue = CardType.Skill;
    private const CardRarity RarityValue = CardRarity.Common;
    private const TargetType TargetTypeValue = TargetType.Self;
    private const bool ShouldShowInCardLibraryValue = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Retain,
        CardKeyword.Exhaust,
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [KnowledgeDemonKeywordHoverTips.FromMaterialize(DynamicVars)];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new MaterializeVar(2),
    ];

    public Manifestation()
        : base(EnergyCostValue, TypeValue, RarityValue, TargetTypeValue, ShouldShowInCardLibraryValue)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;

        await BookLibraryCmd.MaterializeFromLibraryToHand(
            choiceContext,
            Owner,
            DynamicVars[MaterializeVar.DefaultName].IntValue,
            SelectionScreenPrompt,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[MaterializeVar.DefaultName].UpgradeValueBy(1m);
    }
}
