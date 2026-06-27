using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class TwisterStoredDamagePower : KnowledgeDemonPowerModel
{
    private const string StoredDamageVarName = "Amount";

    private sealed class BankingData
    {
        public bool BankingThisTurn;
        public decimal PendingBank;
        public bool CreateUpgradedTwister;
    }

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar(StoredDamageVarName, 0m)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromCard<Twister>()];

    protected override object InitInternalData() => new BankingData();

    private int StoredDamage => Math.Max(0, Amount - 1);

    public override int DisplayAmount => StoredDamage;

    public void EnableBankingThisTurn()
    {
        AssertMutable();
        GetInternalData<BankingData>().BankingThisTurn = true;
    }

    public void MarkCreatesUpgradedTwister()
    {
        AssertMutable();
        GetInternalData<BankingData>().CreateUpgradedTwister = true;
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        SyncStoredDamageVar();
        return Task.CompletedTask;
    }

    public override decimal ModifyHpLostAfterOstyLate(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || amount <= 0m || !props.IsPoweredAttack())
        {
            return amount;
        }

        var data = GetInternalData<BankingData>();
        if (!data.BankingThisTurn)
        {
            return amount;
        }

        data.PendingBank += amount;
        return 0m;
    }

    public override async Task AfterModifyingHpLostAfterOsty()
    {
        var data = GetInternalData<BankingData>();
        if (data.PendingBank <= 0m)
        {
            return;
        }

        await PowerCmd.ModifyAmount(
            new ThrowingPlayerChoiceContext(),
            this,
            data.PendingBank,
            null,
            null);
        data.PendingBank = 0m;
        SyncStoredDamageVar();
        Flash();
    }

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != CombatSide.Player)
        {
            return;
        }

        var storedDamage = StoredDamage;
        if (storedDamage <= 0)
        {
            await PowerCmd.Remove(this);
            return;
        }

        var player = Applier?.Player;
        if (player is null)
        {
            return;
        }

        Flash();
        var twister = combatState.CreateCard<Twister>(player);
        if (GetInternalData<BankingData>().CreateUpgradedTwister && !twister.IsUpgraded)
        {
            CardCmd.Upgrade(twister, CardPreviewStyle.None);
        }

        twister.SetStoredDamage(storedDamage);
        await CardPileCmd.AddGeneratedCardToCombat(twister, PileType.Hand, player);
        await PowerCmd.Remove(this);
    }

    public override Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Player)
        {
            GetInternalData<BankingData>().BankingThisTurn = false;
        }

        return Task.CompletedTask;
    }

    private void SyncStoredDamageVar()
    {
        DynamicVars[StoredDamageVarName].BaseValue = StoredDamage;
    }
}
