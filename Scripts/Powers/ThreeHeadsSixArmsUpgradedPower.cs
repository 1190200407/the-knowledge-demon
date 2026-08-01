using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class ThreeHeadsSixArmsUpgradedPower : KnowledgeDemonPowerModel, IKnowledgeDemonEventListener
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromEnchantment<Adroit>((int)Amount)
            .Concat(HoverTipFactory.FromEnchantment<Momentum>((int)Amount));

    public Task AfterMaterializedFromLibrary(
        PlayerChoiceContext choiceContext,
        Player player,
        IReadOnlyList<CardModel> materialized)
    {
        _ = choiceContext;

        if (Owner.Player != player || Amount <= 0)
        {
            return Task.CompletedTask;
        }

        var triggered = false;
        foreach (var card in materialized)
        {
            if (card.Owner != player)
            {
                continue;
            }

            if (card.Type == CardType.Skill && TryEnchant<Adroit>(card, Amount))
            {
                triggered = true;
                continue;
            }

            if (card.Type == CardType.Attack && TryEnchant<Momentum>(card, Amount))
            {
                triggered = true;
            }
        }

        if (triggered)
        {
            Flash();
        }

        return Task.CompletedTask;
    }

    private static bool TryEnchant<T>(CardModel card, decimal amount)
        where T : EnchantmentModel
    {
        var enchantment = ModelDb.Enchantment<T>();
        if (!enchantment.CanEnchant(card))
        {
            return false;
        }

        CardCmd.Enchant<T>(card, amount);
        return true;
    }
}
