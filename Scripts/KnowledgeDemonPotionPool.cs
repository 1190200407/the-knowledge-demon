using STS2RitsuLib.Scaffolding.Content;

namespace ComicChess.KnowledgeDemon;

public class KnowledgeDemonPotionPool : TypeListPotionPoolModel
{
    // 描述中使用的能量图标。大小为24x24。
    public override string? TextEnergyIconPath => "res://KnowledgeDemon/images/charui/energy_icon.png";
    // tooltip和卡牌左上角的能量图标。大小为74x74。
    public override string? BigEnergyIconPath => "res://KnowledgeDemon/images/charui/energy_icon_big.png";

    public override string EnergyColorName => "knowledge_demon";
}