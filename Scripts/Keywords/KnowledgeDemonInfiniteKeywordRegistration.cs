using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ComicChess.KnowledgeDemon;

[RegisterOwnedCardKeyword(
    "infinite",
    CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.AfterCardDescription)]
public sealed class KnowledgeDemonInfiniteKeywordRegistration;
