using System;
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
using STS2RitsuLib.Keywords;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class TouchGold : KnowledgeDemonCardModel
{
    private const int energyCost = 1;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Uncommon;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;

    private readonly HashSet<CardModel> _returnToHandThisPlay = new();

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [ModKeywordRegistry.GetCardKeyword(KnowledgeDemonKeyword.Choose), CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [KnowledgeDemonKeywordHoverTips.FromChoose()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(8m, ValueProp.Move)];

    public TouchGold()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    public override CardLocation ModifyCardPlayResultLocation(
        CardModel card,
        bool isAutoPlay,
        ResourceInfo resources,
        CardLocation cardLocation)
    {
        if (!_returnToHandThisPlay.Contains(card) || cardLocation.pileType != PileType.Discard)
        {
            return cardLocation;
        }

        return new CardLocation(cardLocation.player, PileType.Hand, cardLocation.position);
    }

    public override Task AfterModifyingCardPlayResultLocation(
        CardModel card,
        CardLocation cardLocation)
    {
        _returnToHandThisPlay.Remove(card);
        return Task.CompletedTask;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(choiceContext);

        var result = await BookLibraryCmd.ChooseFromLibrary(choiceContext, Owner, this);
        await ApplyTouchGoldChooseResult(choiceContext, Owner, result);
    }

    private async Task ApplyTouchGoldChooseResult(
        PlayerChoiceContext choiceContext,
        Player player,
        BookLibraryChooseResult result)
    {
        if (!result.HasCandidates)
        {
            return;
        }

        if (result.Chosen != null)
        {
            _returnToHandThisPlay.Add(result.Chosen);
            await BookLibraryCmd.PlayChosenCard(choiceContext, Owner, result.Chosen, this);
        }

        await BookLibraryCmd.ResolveUnchosenLibraryCandidates(choiceContext, result.Unchosen);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
