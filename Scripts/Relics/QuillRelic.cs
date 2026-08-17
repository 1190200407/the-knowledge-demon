using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterRelic(typeof(KnowledgeDemonRelicPool))]
public sealed class QuillRelic : KnowledgeDemonRelicModel
{
    private bool _triggeredThisCombat;

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override Task BeforeCombatStart()
    {
        _triggeredThisCombat = false;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (_triggeredThisCombat
            || cardPlay.Card.Owner != Owner
            || cardPlay.Card.Type != CardType.Power)
        {
            return;
        }

        _triggeredThisCombat = true;
        Flash();
        await BookLibraryCmd.RecordToLibrary(choiceContext, Owner, cardPlay.Card, 1);
    }
}
