using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class CausalityInversion : KnowledgeDemonCardModel
{
    private const int energyCost = -1;
    private const CardType type = CardType.Power;
    private const CardRarity rarity = CardRarity.Rare;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [KnowledgeDemonKeywordHoverTips.FromMaterializeKeyword()];

    public CausalityInversion()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var xValue = ResolveEnergyXValue();
        var materializeCount = xValue;
        if (IsUpgraded)
        {
            materializeCount++;
        }

        var materialized = await BookLibraryCmd.MaterializeFromLibraryToHand(
            choiceContext,
            Owner,
            materializeCount,
            SelectionScreenPrompt,
            this);

        if (xValue <= 0)
        {
            return;
        }

        foreach (var card in materialized)
        {
            card.EnergyCost.AddThisCombat(-xValue);
            card.InvokeEnergyCostChanged();
        }
    }

    protected override void OnUpgrade()
    {
    }
}
