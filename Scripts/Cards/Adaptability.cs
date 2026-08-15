using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class Adaptability : KnowledgeDemonCardModel, IKnowledgeDemonEventListener
{
    private const int EnergyCostValue = 1;
    private const CardType TypeValue = CardType.Skill;
    private const CardRarity RarityValue = CardRarity.Common;
    private const TargetType TargetTypeValue = TargetType.Self;
    private const bool ShouldShowInCardLibraryValue = true;

    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [KnowledgeDemonKeywordHoverTips.FromRecord()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(4m, ValueProp.Move),
        new IntVar("RecordedBlock", 4),
    ];

    public Adaptability()
        : base(EnergyCostValue, TypeValue, RarityValue, TargetTypeValue, ShouldShowInCardLibraryValue)
    {
    }

    public async Task AfterRecordedToLibrary(
        PlayerChoiceContext? choiceContext,
        Player player,
        CardModel sourceCard,
        IReadOnlyList<CardModel> recordedCopies)
    {
        if (!ReferenceEquals(sourceCard, this) || recordedCopies.Count == 0)
        {
            return;
        }

        await CreatureCmd.GainBlock(
            player.Creature,
            DynamicVars["RecordedBlock"].IntValue * recordedCopies.Count,
            ValueProp.Move,
            null);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
        DynamicVars["RecordedBlock"].UpgradeValueBy(2m);
    }
}
