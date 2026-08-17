using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterRelic(typeof(KnowledgeDemonRelicPool))]
public sealed class InsectSloughRelic : KnowledgeDemonRelicModel, IKnowledgeDemonEventListener
{
    private const int DamageAmount = 3;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [KnowledgeDemonKeywordHoverTips.FromMaterializeKeyword()];

    public async Task AfterMaterializedFromLibrary(
        PlayerChoiceContext choiceContext,
        Player player,
        IReadOnlyList<CardModel> materialized)
    {
        var combatState = Owner.Creature.CombatState;
        if (player != Owner || materialized.Count == 0 || combatState is not { HittableEnemies.Count: > 0 })
        {
            return;
        }

        Flash();
        await CreatureCmd.Damage(
            choiceContext,
            combatState.HittableEnemies,
            DamageAmount * materialized.Count,
            ValueProp.Unpowered,
            Owner.Creature,
            null);
    }
}
