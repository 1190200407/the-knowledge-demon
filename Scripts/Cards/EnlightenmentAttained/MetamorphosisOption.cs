using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class MetamorphosisOption : KnowledgeDemonCardModel, IEnlightenmentAttainedRewardOption
{
    private const int energyCost = -1;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Token;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = false;

    public override int MaxUpgradeLevel => 1;

    public override bool CanBeGeneratedInCombat => false;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.Transform)];

    public MetamorphosisOption()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    public async Task OnChosen()
    {
        var card = (await CardSelectCmd.FromDeckForTransformation(
            Owner,
            new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1))).FirstOrDefault();

        if (card is null)
        {
            return;
        }

        var transformResult = await CardCmd.TransformToRandom(
            card,
            Owner.PlayerRng.Transformations,
            CardPreviewStyle.EventLayout);

        if (IsUpgraded
            && transformResult is { cardAdded: CardModel transformed }
            && transformed.IsUpgradable
            && !transformed.IsUpgraded)
        {
            CardCmd.Upgrade(transformed);
        }
    }
}
