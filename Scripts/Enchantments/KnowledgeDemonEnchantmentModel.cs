using Godot;
using STS2RitsuLib.Scaffolding.Content;

namespace ComicChess.KnowledgeDemon;

public abstract class KnowledgeDemonEnchantmentModel : ModEnchantmentTemplate
{
    public override string? CustomIconPath =>
        ResolveEnchantmentPath("res://KnowledgeDemon/images/enchantments/{0}.png");

    private string? ResolveEnchantmentPath(string format)
    {
        var path = string.Format(format, ResolveEnchantmentKey());
        return ResourceLoader.Exists(path) ? path : null;
    }

    private string ResolveEnchantmentKey() =>
        Id.Entry.ToUpperInvariant().Replace("KNOWLEDGE_DEMON_ENCHANTMENT_", "");
}
