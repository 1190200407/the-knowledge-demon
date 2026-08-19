using Godot;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ComicChess.KnowledgeDemon;

[RegisterPotion(typeof(KnowledgeDemonPotionPool), Inherit = true)]
public abstract class KnowledgeDemonPotionModel : ModPotionTemplate
{
    private const string PotionImagePath = "res://KnowledgeDemon/images/potions/STATUS_POTION.png";
    private const string PotionOutlinePath = "res://KnowledgeDemon/images/potions/STATUS_POTION_outline.png";

    public override string? CustomImagePath =>
        ResolvePotionPath("res://KnowledgeDemon/images/potions/{0}.png", PotionImagePath);

    public override string? CustomOutlinePath =>
        ResolvePotionPath("res://KnowledgeDemon/images/potions/{0}_outline.png", PotionOutlinePath);

    private string ResolvePotionPath(string format, string fallback) =>
        ResourceLoader.Exists(string.Format(format, ResolvePotionKey()))
            ? string.Format(format, ResolvePotionKey())
            : fallback;

    private string ResolvePotionKey() =>
        Id.Entry.ToUpperInvariant().Replace("KNOWLEDGE_DEMON_POTION_", "");
}
