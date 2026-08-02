using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Cards.Transforms;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class EnvironmentalTolerancePower : KnowledgeDemonPowerModel, IKnowledgeDemonEventListener
{
    private const int StatusGenerationThreshold = 5;

    private sealed class Data
    {
        public int generatedStatusCount;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override int DisplayAmount =>
        StatusGenerationThreshold - GetInternalData<Data>().generatedStatusCount;

    protected override object InitInternalData() => new Data();

    public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (Owner.Player is not { } player
            || card.Type != CardType.Status
            || Amount <= 0)
        {
            return;
        }

        await CountGeneratedStatus(player);
    }

    public async Task AfterCardTransformed(Player player, ModCardTransformContext context)
    {
        if (context.Replacement.Type != CardType.Status
            || Amount <= 0)
        {
            return;
        }

        await CountGeneratedStatus(player);
    }

    private async Task CountGeneratedStatus(Player player)
    {
        var data = GetInternalData<Data>();
        data.generatedStatusCount++;
        if (data.generatedStatusCount < StatusGenerationThreshold)
        {
            InvokeDisplayAmountChanged();
            return;
        }

        data.generatedStatusCount = 0;
        InvokeDisplayAmountChanged();
        Flash();
        await KnowledgeDemonCardCmd.AddRandomKnowledgeDemonStatusCardToCombat(player);
    }
}
