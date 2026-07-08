using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class KnowledgeCurse : KnowledgeDemonCardModel
{
    private const int EnergyCostValue = 2;
    private const CardType TypeValue = CardType.Skill;
    private const CardRarity RarityValue = CardRarity.Rare;
    private const TargetType TargetTypeValue = TargetType.Self;
    private const bool ShouldShowInCardLibraryValue = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [ModKeywordRegistry.GetCardKeyword(KnowledgeDemonKeyword.Record), CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        IsUpgraded
            ? [KnowledgeDemonKeywordHoverTips.FromRecord(), KnowledgeDemonKeywordHoverTips.FromMaterialize(DynamicVars), HoverTipFactory.FromCard<GoodGrace>(), HoverTipFactory.FromCard<Collapse>(), HoverTipFactory.FromCard<MindAscension>()]
            : [KnowledgeDemonKeywordHoverTips.FromRecord(), HoverTipFactory.FromCard<GoodGrace>(), HoverTipFactory.FromCard<Collapse>(), HoverTipFactory.FromCard<MindAscension>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new RecordVar(1),
            new MaterializeVar(0),
        ];

    public KnowledgeCurse()
        : base(EnergyCostValue, TypeValue, RarityValue, TargetTypeValue, ShouldShowInCardLibraryValue)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        var recordedCards = new List<CardModel>
        {
            Owner.RunState.CreateCard<GoodGrace>(Owner),
            Owner.RunState.CreateCard<Collapse>(Owner),
            Owner.RunState.CreateCard<MindAscension>(Owner),
        };

        foreach (var card in recordedCards)
        {
            await Cmd.CustomScaledWait(0.1f, 0.25f);
            await BookLibraryCmd.RecordToLibrary(choiceContext, Owner, card, DynamicVars["Record"].IntValue);
        }

        if (!IsUpgraded || DynamicVars["Materialize"].IntValue <= 0)
        {
            return;
        }

        await Cmd.CustomScaledWait(0.5f, 1f);

        await BookLibraryCmd.MaterializeFromLibraryToHand(
            choiceContext,
            Owner,
            DynamicVars["Materialize"].IntValue,
            SelectionScreenPrompt,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[MaterializeVar.DefaultName].BaseValue = 1m;
    }
}
