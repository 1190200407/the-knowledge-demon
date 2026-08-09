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
public sealed class MindAscension : KnowledgeDemonCardModel
{
    public override int MaxUpgradeLevel => 0;

    private const int EnergyCostValue = 0;
    private const CardType TypeValue = CardType.Status;
    private const CardRarity RarityValue = CardRarity.Token;
    private const TargetType TargetTypeValue = TargetType.None;
    private const bool ShouldShowInCardLibraryValue = false;

    public override bool CanBeGeneratedInCombat => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1),
    ];

    public MindAscension()
        : base(EnergyCostValue, TypeValue, RarityValue, TargetTypeValue, ShouldShowInCardLibraryValue)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
    }
}
