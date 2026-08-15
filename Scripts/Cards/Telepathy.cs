using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class Telepathy : KnowledgeDemonCardModel
{
    private const int EnergyCostValue = 3;
    private const CardType TypeValue = CardType.Power;
    private const CardRarity RarityValue = CardRarity.Rare;
    private const TargetType TargetTypeValue = TargetType.Self;
    private const bool ShouldShowInCardLibraryValue = true;

    public override CardMultiplayerConstraint MultiplayerConstraint =>
        CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        ModKeywordRegistry.GetCardKeyword(KnowledgeDemonKeyword.Unique),
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromRelic<CognitionVesselRelic>();

    public Telepathy()
        : base(EnergyCostValue, TypeValue, RarityValue, TargetTypeValue, ShouldShowInCardLibraryValue)
    {
    }

    protected override async Task OnPlay(
        MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        _ = cardPlay;

        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);

        if (CombatState is null)
        {
            return;
        }

        var alliedPlayers = CombatState.PlayerCreatures
            .Where(creature => creature.IsAlive && creature.Player is not null)
            .Select(creature => creature.Player!)
            .Distinct()
            .ToList();

        var sharedAnyPlayer = false;
        foreach (var player in alliedPlayers)
        {
            if (BookLibraryUtility.PlayerHasBookLibraryRelic(player))
            {
                continue;
            }

            var temporaryRelic = ModelDb.Relic<CognitionVesselRelic>().ToMutable();
            temporaryRelic.IsWax = true;
            await RelicCmd.Obtain(temporaryRelic, player);
            sharedAnyPlayer = true;
        }

        if (sharedAnyPlayer)
        {
            await PowerCmd.Apply<TelepathyPower>(
                choiceContext,
                Owner.Creature,
                1m,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
