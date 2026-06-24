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
public sealed class Eureka : KnowledgeDemonCardModel
{
    private const int energyCost = -1;
    private const CardType type = CardType.Status;
    private const CardRarity rarity = CardRarity.Rare;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;
    private const string EnergyVarName = "Amount";

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        EnergyHoverTip,
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(EnergyVarName, 1m),
        new CardsVar(1),
    ];

    public Eureka()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    public override async Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        AbstractModel? clonedBy)
    {
        _ = clonedBy;

        if (card != this || oldPileType == PileType.Hand || Pile?.Type != PileType.Hand || Owner is null)
        {
            return;
        }

        await PlayerCmd.GainEnergy(DynamicVars[EnergyVarName].BaseValue, Owner);
        await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[EnergyVarName].UpgradeValueBy(1m);
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
