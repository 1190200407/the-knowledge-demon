using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models;

namespace ComicChess.KnowledgeDemon;

[RegisterSingleton]
public sealed class KnowledgeDemonRuleFtueSingleton : HookedSingletonModel
{
    public KnowledgeDemonRuleFtueSingleton()
        : base(HookType.None)
    {
    }
}
