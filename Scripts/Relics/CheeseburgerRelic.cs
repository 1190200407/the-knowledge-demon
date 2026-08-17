using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterRelic(typeof(KnowledgeDemonRelicPool))]
public sealed class CheeseburgerRelic : KnowledgeDemonRelicModel, IKnowledgeDemonEventListener
{
    private const int BlockAmount = 3;

    public override RelicRarity Rarity => RelicRarity.Common;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [KnowledgeDemonKeywordHoverTips.FromKnowledgeOverload()];

    public async Task AfterKnowledgeOverloadTriggered(
        PlayerChoiceContext choiceContext,
        Player player,
        AbstractModel? source)
    {
        _ = source;

        if (player != Owner)
        {
            return;
        }

        Flash();
        await CreatureCmd.GainBlock(Owner.Creature, BlockAmount, ValueProp.Unpowered, null);
    }
}
