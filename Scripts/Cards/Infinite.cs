using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class Infinite : KnowledgeDemonCardModel
{
    private const int EnergyCostValue = 0;
    private const CardType TypeValue = CardType.Status;
    private const CardRarity RarityValue = CardRarity.Ancient;
    private const TargetType TargetTypeValue = TargetType.Self;
    private const bool ShouldShowInCardLibraryValue = true;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.Transform), HoverTipFactory.FromKeyword(ModKeywordRegistry.GetCardKeyword(KnowledgeDemonKeyword.Unique))];

    public Infinite()
        : base(EnergyCostValue, TypeValue, RarityValue, TargetTypeValue, ShouldShowInCardLibraryValue)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var candidates = TransformOptionUtility
            .GetInfiniteTransformationStatusCandidates(Owner)
            .Select(card => Owner.RunState.CreateCard(card, Owner))
            .ToList();

        if (candidates.Count == 0)
        {
            return;
        }

        var chosen = (await CardSelectCmd.FromSimpleGrid(
                choiceContext,
                candidates,
                Owner,
                new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1)))
            .FirstOrDefault();
        if (chosen is null)
        {
            return;
        }

        var replacement = chosen.CreateClone();
        var transformed = await CardCmd.Transform(this, replacement, CardPreviewStyle.HorizontalLayout);
        if (transformed?.cardAdded is { } transformedCard)
        {
            await CardPileCmd.Add(transformedCard, PileType.Hand);
        }
    }
}
