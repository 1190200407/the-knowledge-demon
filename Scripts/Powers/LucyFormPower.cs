using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class LucyFormPower : KnowledgeDemonPowerModel
{
    private bool _isTransforming;

    [SavedProperty]
    public int CreatedLucyFormCount { get; set; }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<LucyFormState>()];

    public override async Task AfterCardGeneratedForCombat(
        CardModel card,
        Player? creator)
    {
        if (_isTransforming
            || creator != Owner.Player
            || card.Owner != Owner.Player
            || card is LucyFormState
            || card.Pile is not { IsCombatPile: true })
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
        var stage = Math.Clamp(CreatedLucyFormCount + 1, 1, 10);
        replacement.InitializeStage(stage);
        CreatedLucyFormCount = stage;

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
