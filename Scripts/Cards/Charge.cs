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

    public override int MaxUpgradeLevel => 0;

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
        new DamageVar(25m, ValueProp.Unpowered),
        new PowerVar<StrengthPower>(1m),
        new DynamicVar(RemainingRetainsVarName, requiredRetains),
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

        if (player != Owner || !retainedCards.Contains(this))
        {
            return;
        }

        RetainedCount++;

        await PowerCmd.Apply<StrengthPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["StrengthPower"].BaseValue,
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

    private void SyncRemainingRetains()
    {
        DynamicVars[RemainingRetainsVarName].BaseValue = Math.Max(0, requiredRetains - RetainedCount);

        if (Pile?.Type == PileType.Hand)
        {
            NCard.FindOnTable(this)?.UpdateVisuals(PileType.Hand, CardPreviewMode.Normal);
        }
    }
}
