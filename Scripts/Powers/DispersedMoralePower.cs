using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class DispersedMoralePower : TemporaryStrengthPower, IModPowerAssetOverrides
{
    public PowerAssetProfile AssetProfile => new(
        IconPath: "res://KnowledgeDemon/images/powers/DISPERSED_MORALE.png",
        BigIconPath: "res://KnowledgeDemon/images/powers/big/DISPERSED_MORALE.png");

    public string? CustomIconPath => "res://KnowledgeDemon/images/powers/DISPERSED_MORALE.png";
    public string? CustomBigIconPath => "res://KnowledgeDemon/images/powers/big/DISPERSED_MORALE.png";

    public override AbstractModel OriginModel => ModelDb.Card<DispersedMorale>();

    protected override bool IsPositive => false;
}
