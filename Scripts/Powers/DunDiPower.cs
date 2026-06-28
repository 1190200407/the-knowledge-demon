using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class DunDiPower : KnowledgeDemonPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override LocString Title => DunDiPowerShared.Title;

    public override string? CustomIconPath => "res://images/atlases/power_atlas.sprites/burrowed_power.tres";

    public override string? CustomBigIconPath => "res://images/powers/burrowed_power.png";

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.Block)];

    public override bool ShouldClearBlock(Creature creature) => Owner != creature;

    public override async Task AfterBlockBroken(Creature creature)
    {
        if (creature != Owner)
        {
            return;
        }

        Flash();
        await PowerCmd.Remove(this);
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        await CreatureCmd.LoseBlock(oldOwner, 999999999m);
    }
}

internal static class DunDiPowerShared
{
    internal static readonly LocString Title = new("powers", "KNOWLEDGE_DEMON_POWER_DUN_DI_POWER.title");
}
