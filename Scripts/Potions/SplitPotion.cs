using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPotion(typeof(KnowledgeDemonPotionPool))]
public sealed class SplitPotion : KnowledgeDemonPotionModel
{
    public override PotionRarity Rarity => PotionRarity.Rare;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.AnyPlayer;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new MaterializeVar(1),
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        KnowledgeDemonKeywordHoverTips.FromMaterialize(DynamicVars),
    ];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (target?.Player is not Player player)
        {
            return;
        }

        var prompt = BookLibraryUtility.PlayerHasBookLibraryAccess(player) ? SelectionScreenPrompt : 
            new LocString("potions", "KNOWLEDGE_DEMON_POTION_SPLIT_POTION.selectionScreenPrompt2");
        var selection = (await KnowledgeDemonCardSelectCmd.FromBookLibraryAndHand(
            choiceContext,
            player,
            new CardSelectorPrefs(prompt, 1, 1),
            null,
            this)).FirstOrDefault();

        if (selection is null)
        {
            return;
        }

        await BookLibraryCmd.DuplicateCardToCurrentPile(Owner, selection);
    }
}
