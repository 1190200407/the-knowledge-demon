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
public sealed class Adaptability : KnowledgeDemonCardModel
{
    private const int EnergyCostValue = 0;
    private const CardType TypeValue = CardType.Skill;
    private const CardRarity RarityValue = CardRarity.Common;
    private const TargetType TargetTypeValue = TargetType.Self;
    private const bool ShouldShowInCardLibraryValue = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        ModKeywordRegistry.GetCardKeyword(KnowledgeDemonKeyword.Unique),
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        EnergyHoverTip,
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1),
        new CardsVar(1),
    ];

    public Adaptability()
        : base(EnergyCostValue, TypeValue, RarityValue, TargetTypeValue, ShouldShowInCardLibraryValue)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = cardPlay;
        var playerCombatState = Owner.PlayerCombatState;
        if (playerCombatState is null)
        {
            return;
        }

        if (playerCombatState.Energy == 0)
        {
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
            return;
        }

        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
