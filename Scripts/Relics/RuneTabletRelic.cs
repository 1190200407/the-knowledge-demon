using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterRelic(typeof(KnowledgeDemonRelicPool))]
public sealed class RuneTabletRelic : KnowledgeDemonRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromEnchantment<Incantation>();

    public override async Task AfterObtained()
    {
        var enchantment = ModelDb.Enchantment<Incantation>();
        var selectedCards = await CardSelectCmd.FromDeckForEnchantment(
            prefs: new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, DynamicVars.Cards.IntValue),
            player: Owner,
            enchantment: enchantment,
            amount: 1);

        foreach (var card in selectedCards)
        {
            CardCmd.Enchant<Incantation>(card, 1m);

            var enchantVfx = NCardEnchantVfx.Create(card);
            if (enchantVfx is not null)
            {
                NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(enchantVfx);
            }
        }
    }
}
