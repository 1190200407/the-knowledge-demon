using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class PrecognitionRunePower : KnowledgeDemonPowerModel
{
    private sealed class ConversionData
    {
        public decimal PendingDisintegration;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<DisintegrationPower>(),
    ];

    protected override object InitInternalData() => new ConversionData();

    public override decimal ModifyHpLostAfterOstyLate(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        _ = dealer;
        _ = cardSource;

        if (target != Owner || amount <= 0m)
        {
            return amount;
        }

        var disintegrationAmount = amount / 2m;
        if (disintegrationAmount > 0m)
        {
            GetInternalData<ConversionData>().PendingDisintegration += disintegrationAmount;
        }

        return 0m;
    }

    public override async Task AfterModifyingHpLostAfterOsty()
    {
        var data = GetInternalData<ConversionData>();
        if (data.PendingDisintegration <= 0m)
        {
            return;
        }

        await PowerCmd.Apply<DisintegrationPower>(
            new ThrowingPlayerChoiceContext(),
            Owner,
            data.PendingDisintegration,
            Owner,
            null);
        data.PendingDisintegration = 0m;
        Flash();
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        _ = participants;

        if (side == CombatSide.Enemy)
        {
            await PowerCmd.Remove(this);
        }
    }
}
