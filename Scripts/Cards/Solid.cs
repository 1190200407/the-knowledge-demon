using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class Solid : KnowledgeDemonCardModel
{
    private const int energyCost = -1;
    private const CardType type = CardType.Status;
    private const CardRarity rarity = CardRarity.Uncommon;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;
    private const string AppliedDexterityVarName = "AppliedDexterity";

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Unplayable,
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<DexterityPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<DexterityPower>(1m),
        new DynamicVar(AppliedDexterityVarName, 0m),
    ];

    public Solid()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    public override async Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        AbstractModel? clonedBy)
    {
        _ = oldPileType;
        _ = clonedBy;

        if (card != this || Owner is null)
        {
            return;
        }

        var inHandOrLibrary = Pile is { } pile
            && (pile.Type == PileType.Hand || BookLibraryUtility.IsBookLibraryPile(pile.Type));
        var applied = DynamicVars[AppliedDexterityVarName].IntValue;

        if (inHandOrLibrary && applied == 0)
        {
            await PowerCmd.Apply<DexterityPower>(
                new ThrowingPlayerChoiceContext(),
                Owner.Creature,
                1m,
                Owner.Creature,
                this);
            DynamicVars[AppliedDexterityVarName].BaseValue = 1m;
        }
        else if (!inHandOrLibrary && applied > 0)
        {
            await PowerCmd.Apply<DexterityPower>(
                new ThrowingPlayerChoiceContext(),
                Owner.Creature,
                -1m,
                Owner.Creature,
                this);
            DynamicVars[AppliedDexterityVarName].BaseValue = 0m;
        }
    }
}
