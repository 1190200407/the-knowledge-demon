using Godot;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ComicChess.KnowledgeDemon;

[RegisterRelic(typeof(KnowledgeDemonRelicPool), Inherit = true)]
public abstract class KnowledgeDemonRelicModel : ModRelicTemplate
{
    private const string RelicIconPath = "res://KnowledgeDemon/images/relics/relic.png";
    private const string RelicBigIconPath = "res://KnowledgeDemon/images/relics/big/relic.png";
    private const string RelicOutlinePath = "res://KnowledgeDemon/images/relics/relic_outline.png";

    public override string? CustomIconPath =>
        ResolveRelicPath("res://KnowledgeDemon/images/relics/{0}.png", RelicIconPath);

    public override string? CustomIconOutlinePath =>
        ResolveRelicPath("res://KnowledgeDemon/images/relics/{0}_outline.png", RelicOutlinePath);

    public override string? CustomBigIconPath =>
        ResolveRelicPath("res://KnowledgeDemon/images/relics/big/{0}.png", RelicBigIconPath);

    private string ResolveRelicPath(string format, string fallback) =>
        ResourceLoader.Exists(string.Format(format, ResolveRelicKey()))
            ? string.Format(format, ResolveRelicKey())
            : fallback;

    private string ResolveRelicKey() =>
        Id.Entry.ToUpperInvariant().Replace("KNOWLEDGE_DEMON_RELIC_", "").Replace("_RELIC", "");
}
