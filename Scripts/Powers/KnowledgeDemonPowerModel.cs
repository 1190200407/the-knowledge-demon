using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Scaffolding.Content;

namespace ComicChess.KnowledgeDemon;

public abstract class KnowledgeDemonPowerModel : ModPowerTemplate
{
    private const string PowerIconPath = "res://KnowledgeDemon/images/powers/power.png";

    public override PowerAssetProfile AssetProfile => new(IconPath: PowerIconPath, BigIconPath: PowerIconPath);
}
