using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class EnlightenmentAttainedUpgradedPower : KnowledgeDemonPowerModel
{
    private const string HealVarName = "Heal";
    private const string GoldVarName = "Gold";

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override LocString Title => EnlightenmentAttainedPowerShared.Title;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar(HealVarName, 10m),
        new IntVar(GoldVarName, 20m),
    ];

    public override Task AfterCombatEnd(CombatRoom room)
    {
        var player = Owner.Player!;
        room.AddExtraReward(player, new EnlightenmentAttainedUpgradedReward(player));
        return Task.CompletedTask;
    }
}
