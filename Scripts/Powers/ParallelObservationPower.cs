using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class ParallelObservationPower : KnowledgeDemonPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override LocString Title => ParallelObservationPowerShared.Title;
}

[RegisterPower]
public sealed class ParallelObservationUpgradedPower : KnowledgeDemonPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override LocString Title => ParallelObservationPowerShared.Title;
}

internal static class ParallelObservationPowerShared
{
    internal static readonly LocString Title =
        new("powers", "KNOWLEDGE_DEMON_POWER_PARALLEL_OBSERVATION_POWER.title");
}
