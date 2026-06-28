using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class PlanThenActExtraTurnPower : KnowledgeDemonPowerModel
{
    private bool _usedThisCombat;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    private bool UsedThisCombat
    {
        get => _usedThisCombat;
        set
        {
            AssertMutable();
            _usedThisCombat = value;
        }
    }

    public override bool ShouldTakeExtraTurn(Player player) =>
        !UsedThisCombat && player == Owner.Player;

    public override Task AfterTakingExtraTurn(Player player)
    {
        if (player == Owner.Player)
        {
            Flash();
            UsedThisCombat = true;
        }

        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _ = room;
        UsedThisCombat = false;
        return Task.CompletedTask;
    }
}
