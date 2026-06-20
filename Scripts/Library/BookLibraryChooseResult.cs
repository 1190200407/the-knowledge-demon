using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Models;

namespace ComicChess.KnowledgeDemon;

/// <summary>藏书库抉择结果：本次展示的候选、玩家选中的牌与未选中的候选。</summary>
public sealed class BookLibraryChooseResult
{
    public static readonly BookLibraryChooseResult Empty = new(null, []);

    public CardModel? Chosen { get; }

    public IReadOnlyList<CardModel> Candidates { get; }

    public IReadOnlyList<CardModel> Unchosen { get; }

    public bool HasCandidates => Candidates.Count > 0;

    public BookLibraryChooseResult(CardModel? chosen, IReadOnlyList<CardModel> candidates)
    {
        Chosen = chosen;
        Candidates = candidates;
        Unchosen = candidates.Where(c => c != chosen).ToList();
    }
}
