using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class EnlightenmentAttainedPower : KnowledgeDemonPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override LocString Title => EnlightenmentAttainedPowerShared.Title;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        EnlightenmentAttainedOptionHoverTips.All(false);

    public override Task AfterCombatEnd(CombatRoom room)
    {
        var player = Owner.Player!;
        room.AddExtraReward(player, new EnlightenmentAttainedReward(player));
        return Task.CompletedTask;
    }
}
