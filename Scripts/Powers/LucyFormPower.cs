using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class LucyFormPower : KnowledgeDemonPowerModel
{
    private bool _isTransforming;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<LucyFormState>()];

    public override async Task AfterCardChangedPilesLate(
        CardModel card,
        PileType oldPileType,
        AbstractModel? clonedBy)
    {
        _ = clonedBy;

        if (_isTransforming
            || oldPileType != PileType.None
            || card.Owner != Owner.Player
            || card is LucyFormState
            || card.Pile is not { IsCombatPile: true })
        {
            return;
        }

        var generatedEntry = CombatManager.Instance.History.Entries
            .OfType<CardGeneratedEntry>()
            .LastOrDefault(entry => ReferenceEquals(entry.Card, card));
        if (generatedEntry?.Creator != Owner.Player)
        {
            return;
        }

        var next = Amount - 1;
        SetAmount(next);
        if (next > 0)
        {
            return;
        }

        Flash();

        var replacement = card.CardScope?.CreateCard<LucyFormState>(card.Owner)
            ?? card.Owner.RunState.CreateCard<LucyFormState>(card.Owner);

        _isTransforming = true;
        try
        {
            await BookLibraryUtility.TransformCard(card, replacement);
        }
        finally
        {
            _isTransforming = false;
        }

        SetAmount(7);
    }
}
