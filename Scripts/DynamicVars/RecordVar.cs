using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace ComicChess.KnowledgeDemon;

public sealed class RecordVar : DynamicVar
{
    public const string DefaultName = "Record";

    public RecordVar(int record)
        : base(DefaultName, record)
    {
    }

    public RecordVar(string name, int record)
        : base(name, record)
    {
    }
}
