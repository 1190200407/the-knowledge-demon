using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class DispersedMoralePower : TemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<DispersedMorale>();

    protected override bool IsPositive => false;
}
