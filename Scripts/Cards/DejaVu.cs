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
public sealed class DejaVu : KnowledgeDemonCardModel
{
    private const int energyCost = 1;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Rare;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        KnowledgeDemonKeywordHoverTips.FromMaterialize(DynamicVars),
        HoverTipFactory.FromKeyword(CardKeyword.Sly),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new MaterializeVar(1)];

    public DejaVu()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var materialized = await BookLibraryCmd.MaterializeFromLibraryToHand(
            choiceContext,
            Owner,
            DynamicVars[MaterializeVar.DefaultName].IntValue,
            SelectionScreenPrompt,
            this);

        foreach (var card in materialized)
        {
            card.AddKeyword(CardKeyword.Sly);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars[MaterializeVar.DefaultName].UpgradeValueBy(1m);
    }
}
