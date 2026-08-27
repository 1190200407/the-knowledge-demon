using Godot;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace ComicChess.KnowledgeDemon;

public class KnowledgeDemonCardPool : TypeListCardPoolModel
{
    // 卡池的ID。必须唯一防撞车。
    public override string Title => "knowledge_demon";
    public override string EnergyColorName => "knowledge_demon";

    // 描述中使用的能量图标。大小为24x24。
    public override string? TextEnergyIconPath => "res://KnowledgeDemon/images/ui/knowledge_demon_orb_text.png";
    // tooltip和卡牌左上角的能量图标。大小为74x74。
    public override string? BigEnergyIconPath => "res://KnowledgeDemon/images/ui/knowledge_demon_orb_big.png";

    // 卡池的主题色。rgb(10, 61, 21)
    public override Color DeckEntryCardColor => new(10f / 255f, 61f / 255f, 21f / 255f);
    // 能量表盘文字轮廓颜色rgb(10, 61, 21)
    public override Color EnergyOutlineColor => new(10f / 255f, 61f / 255f, 21f / 255f);

    // 根据你使用的卡框决定使用哪个Material rgb(112, 150, 51)
    private static readonly Material? _poolFrameMaterial = MaterialUtils.CreateReplaceHueShaderMaterial(112f / 255f, 150f / 255f, 51f / 255f); // 如果你使用原版卡框，使用这个直接替换色调。
    // private static readonly Material? _poolFrameMaterial = MaterialUtils.CreateRgbShaderMaterial(0.5f, 0.5f, 1f); // 使用原版卡框替换色调。除非你的版本没有CreateReplaceHueShaderMaterial函数，否则应使用上面那种
    // private static readonly Material? _poolFrameMaterial = MaterialUtils.CreateUnmodulatedHsvShaderMaterial(); // 如果你是自定义卡框，使用这个
    public override Material? PoolFrameMaterial => _poolFrameMaterial;

    // 卡池是否是无色。例如事件、状态等卡池就是无色的。
    public override bool IsColorless => false;
}