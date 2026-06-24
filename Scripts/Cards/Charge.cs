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
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class Charge : KnowledgeDemonCardModel
{
    private const int energyCost = -1;
    private const CardType type = CardType.Status;
    private const CardRarity rarity = CardRarity.Uncommon;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;
    private const int requiredRetains = 3;
    private const string RemainingRetainsVarName = "RemainingRetains";
    private const string AppliedDexterityVarName = "AppliedDexterity";

    private int _retainedCount;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Retain,
        CardKeyword.Unplayable,
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(20m, ValueProp.Unpowered),
        new DynamicVar(RemainingRetainsVarName, requiredRetains),
        new DynamicVar(AppliedDexterityVarName, 0m),
    ];

    private int RetainedCount
    {
        get => _retainedCount;
        set
        {
            AssertMutable();
            _retainedCount = value;
            SyncRemainingRetains();
        }
    }

    public Charge()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
        SyncRemainingRetains();
    }

    public override async Task AfterFlush(
        PlayerChoiceContext choiceContext,
        Player player,
        IReadOnlyCollection<CardModel> flushedCards,
        IReadOnlyCollection<CardModel> retainedCards)
    {
        _ = flushedCards;

        if (player != Owner)
        {
            await EnsureDexterityState(choiceContext);
            return;
        }

        await EnsureDexterityState(choiceContext);

        if (!retainedCards.Contains(this))
        {
            return;
        }

        RetainedCount++;

        await PowerCmd.Apply<StrengthPower>(
            choiceContext,
            Owner.Creature,
            1m,
            Owner.Creature,
            this);

        if (RetainedCount < requiredRetains)
        {
            return;
        }

        if (CombatState is { HittableEnemies.Count: > 0 })
        {
            await CreatureCmd.Damage(
                choiceContext,
                CombatState.HittableEnemies,
                DynamicVars.Damage.BaseValue,
                ValueProp.Unpowered,
                Owner.Creature,
                this);
        }

        await CardCmd.Exhaust(choiceContext, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(10m);
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

        await EnsureDexterityState(new ThrowingPlayerChoiceContext());
    }

    private void SyncRemainingRetains()
    {
        DynamicVars[RemainingRetainsVarName].BaseValue = Math.Max(0, requiredRetains - RetainedCount);

        if (Pile?.Type == PileType.Hand)
        {
            NCard.FindOnTable(this)?.UpdateVisuals(PileType.Hand, CardPreviewMode.Normal);
        }
    }

    private async Task EnsureDexterityState(PlayerChoiceContext choiceContext)
    {
        if (Owner is null)
        {
            return;
        }

        var shouldApply = Pile is { } pile
            && (pile.Type == PileType.Hand || BookLibraryUtility.IsBookLibraryPile(pile.Type));
        var applied = DynamicVars[AppliedDexterityVarName].IntValue;

        if (shouldApply && applied == 0)
        {
            await PowerCmd.Apply<DexterityPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
            DynamicVars[AppliedDexterityVarName].BaseValue = 1m;
            return;
        }

        if (!shouldApply && applied > 0)
        {
            await PowerCmd.Apply<DexterityPower>(choiceContext, Owner.Creature, -1m, Owner.Creature, this);
            DynamicVars[AppliedDexterityVarName].BaseValue = 0m;
        }
    }
}
