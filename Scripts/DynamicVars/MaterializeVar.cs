using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace ComicChess.KnowledgeDemon;

public sealed class MaterializeVar : DynamicVar
{
    public const string DefaultName = "Materialize";

    public MaterializeVar(int materialize)
        : this(DefaultName, materialize)
    {
    }

    public MaterializeVar(string name, int materialize)
        : base(name, materialize)
    {
    }
}
