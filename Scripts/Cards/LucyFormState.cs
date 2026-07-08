using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class LucyFormState : KnowledgeDemonCardModel
{
    private const int EnergyCostValue = 0;
    private const CardType TypeValue = CardType.Attack;
    private const CardRarity RarityValue = CardRarity.Token;
    private const TargetType TargetTypeValue = TargetType.AnyEnemy;
    private const bool ShouldShowInCardLibraryValue = false;
    public override bool CanBeGeneratedInCombat => false;

    private const string DamageKey = "LucyDamage";

    public override int MaxUpgradeLevel => 0;

    [SavedProperty]
    public int Stage { get; set; }

    public override string Title
    {
        get
        {
            var baseTitle = TitleLocString.GetFormattedText();
            if (!IsInCombat)
            {
                return baseTitle;
            }
            return $"{baseTitle}{GetStage() * 10}%";
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        ModCardVars.ComputedDamage(
            DamageKey,
            7m,
            (card, _) => card is LucyFormState lucy ? lucy.GetDamageValue() : 7m,
            ValueProp.Move),
    ];

    public LucyFormState()
        : base(EnergyCostValue, TypeValue, RarityValue, TargetTypeValue, ShouldShowInCardLibraryValue)
    {
    }

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (!ReferenceEquals(card, this))
        {
            return Task.CompletedTask;
        }

        InheritStageFromCloneIfNeeded();
        SyncCombatCost();
        return Task.CompletedTask;
    }

    public override Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (!ReferenceEquals(card, this) || creator != Owner)
        {
            return Task.CompletedTask;
        }

        InheritStageFromCloneIfNeeded();
        SyncCombatCost();
        return Task.CompletedTask;
    }

    public void InitializeStage(int stage)
    {
        Stage = Math.Clamp(stage, 1, 10);
        SyncCombatCost();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        var damage = ((ComputedDynamicVar)DynamicVars[DamageKey]).Calculate(cardPlay.Target);
        await DamageCmd.Attack(damage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    private void SyncCombatCost()
    {
        var desiredCost = GetStage() - 1;
        EnergyCost.SetThisCombat(desiredCost);
        InvokeEnergyCostChanged();
    }

    private decimal GetDamageValue()
    {
        var stage = GetStage();
        return 7m * (decimal)Math.Pow(2d, stage - 1);
    }

    private int GetStage()
    {
        if (Stage > 0)
        {
            return Stage;
        }

        if (Owner is null)
        {
            return 1;
        }

        var generatedEntries = CombatManager.Instance.History.Entries
            .OfType<CardGeneratedEntry>()
            .Where(entry => entry.Creator == Owner)
            .Where(entry => ReferenceEquals(entry.Card, this) || ReferenceEquals(entry.Card.CloneOf, this))
            .ToList();
        if (generatedEntries.Count > 0)
        {
            var index = CombatManager.Instance.History.Entries
                .OfType<CardGeneratedEntry>()
                .Where(entry => entry.Creator == Owner)
                .TakeWhile(entry => !ReferenceEquals(entry, generatedEntries[0]))
                .Count(entry => entry.Card is LucyFormState) + 1;
            return Math.Clamp(index, 1, 10);
        }

        var fallbackCount = CombatManager.Instance.History.Entries
            .OfType<CardGeneratedEntry>()
            .Count(entry => entry.Creator == Owner && entry.Card is LucyFormState);
        return Math.Clamp(Math.Max(1, fallbackCount), 1, 10);
    }

    private void InheritStageFromCloneIfNeeded()
    {
        if (Stage > 0 || CloneOf is not LucyFormState source || source.Stage <= 0)
        {
            return;
        }

        Stage = source.Stage;
    }
}
