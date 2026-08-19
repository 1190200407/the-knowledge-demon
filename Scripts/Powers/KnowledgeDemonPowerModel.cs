using Godot;
using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Scaffolding.Content;

namespace ComicChess.KnowledgeDemon;

public abstract class KnowledgeDemonPowerModel : ModPowerTemplate
{
    private const string PowerIconPath = "res://KnowledgeDemon/images/powers/power.png";
    private const string PowerBigIconPath = "res://KnowledgeDemon/images/powers/big/power.png";

    public override string? CustomIconPath =>
    ResolvePowerPath("res://KnowledgeDemon/images/powers/{0}.png", PowerIconPath);

    public override string? CustomBigIconPath =>
        ResolvePowerPath("res://KnowledgeDemon/images/powers/big/{0}.png", PowerBigIconPath);

    private string ResolvePowerPath(string format, string fallback) =>
        ResourceLoader.Exists(string.Format(format, ResolvePowerKey()))
            ? string.Format(format, ResolvePowerKey())
            : fallback;

    private string ResolvePowerKey() =>
        Id.Entry.ToUpperInvariant().Replace("KNOWLEDGE_DEMON_POWER_", "").Replace("_POWER", "");

    public override PowerAssetProfile AssetProfile => new(IconPath: PowerIconPath, BigIconPath: PowerIconPath);
}
