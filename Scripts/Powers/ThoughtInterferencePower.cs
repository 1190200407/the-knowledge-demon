using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class ThoughtInterferencePower : KnowledgeDemonPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<string> RegisteredKeywordIds =>
        [KnowledgeDemonKeyword.Record];

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        _ = participants;
        _ = combatState;

        if (side != CombatSide.Player)
        {
            return;
        }

        var player = Owner.Player;
        if (player is null)
        {
            return;
        }

        var hand = PileType.Hand.GetPile(player);
        if (hand.Cards.Count == 0)
        {
            return;
        }

        var selected = (await CardSelectCmd.FromHand(
            choiceContext,
            player,
            new CardSelectorPrefs(SelectionScreenPrompt, 1),
            null,
            this)).FirstOrDefault();
        if (selected is null)
        {
            return;
        }

        Flash();
        await BookLibraryCmd.RecordToLibrary(choiceContext, player, selected, 1);
        await CardCmd.Exhaust(choiceContext, selected);
    }
}
