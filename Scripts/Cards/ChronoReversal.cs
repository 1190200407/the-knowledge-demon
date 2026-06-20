using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class ChronoReversal : KnowledgeDemonCardModel
{
    private const int energyCost = 1;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Uncommon;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;
    private const string DiscountVarName = "Discount";

    private ICombatState? _trackedCombatState;
    private bool _firstMaterializeDiscountUsed;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [KnowledgeDemonKeywordHoverTips.FromMaterialize(DynamicVars)];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new MaterializeVar(1),
        new DynamicVar(DiscountVarName, 1m),
    ];

    protected override bool ShouldGlowGoldInternal
    {
        get
        {
            var combatState = Owner?.Creature.CombatState;
            if (combatState is null)
            {
                return false;
            }

            if (_trackedCombatState != combatState)
            {
                return true;
            }

            return !_firstMaterializeDiscountUsed;
        }
    }

    public ChronoReversal()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = Owner.Creature.CombatState;
        var applyDiscount = combatState is not null && TryBeginDiscountForCombat(combatState);

        var materialized = await BookLibraryCmd.MaterializeFromLibraryToHand(
            choiceContext,
            Owner,
            DynamicVars[MaterializeVar.DefaultName].IntValue,
            SelectionScreenPrompt,
            this);

        if (applyDiscount && materialized.Count > 0)
        {
            materialized[0].EnergyCost.AddThisCombat(
                -DynamicVars[DiscountVarName].IntValue,
                reduceOnly: true);
            MarkDiscountUsed();
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars[DiscountVarName].UpgradeValueBy(1m);
    }

    private bool TryBeginDiscountForCombat(ICombatState combatState)
    {
        if (_trackedCombatState != combatState)
        {
            _trackedCombatState = combatState;
            _firstMaterializeDiscountUsed = false;
        }

        return !_firstMaterializeDiscountUsed;
    }

    private void MarkDiscountUsed()
    {
        _firstMaterializeDiscountUsed = true;
        NCard.FindOnTable(this)?.UpdateVisuals(PileType.Hand, CardPreviewMode.Normal);
    }
}
