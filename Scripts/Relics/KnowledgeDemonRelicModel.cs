using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ComicChess.KnowledgeDemon;

[RegisterRelic(typeof(KnowledgeDemonRelicPool), Inherit = true)]
public abstract class KnowledgeDemonRelicModel : ModRelicTemplate
{
    private const string RelicIconPath = "res://KnowledgeDemon/images/relics/relic.png";

    public override string? CustomIconPath => RelicIconPath;

    public override string? CustomIconOutlinePath => "res://KnowledgeDemon/images/relics/relic_outline.png";

    public override string? CustomBigIconPath => "res://KnowledgeDemon/images/relics/big/relic.png";
}
