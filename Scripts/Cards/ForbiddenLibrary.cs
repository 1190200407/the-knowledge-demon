using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class ForbiddenLibrary : KnowledgeDemonCardModel
{
    private const int energyCost = 2;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Rare;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;
    private const string CalculatedHitsKey = "CalculatedHits";

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [ModKeywordRegistry.GetCardKeyword(KnowledgeDemonKeyword.Unique)];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10m, ValueProp.Move),
        new CalculationBaseVar(0m),
        new CalculationExtraVar(1m),
        new CalculatedVar(CalculatedHitsKey).WithMultiplier((card, _) =>
            BookLibraryUtility.TryGetLibraryPile(card.Owner)?.Cards.Count ?? 0),
    ];

    public ForbiddenLibrary()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var libraryPile = BookLibraryUtility.TryGetLibraryPile(Owner);
        if (libraryPile is null || libraryPile.Cards.Count == 0)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        var toExhaust = libraryPile.Cards.ToList();
        foreach (var card in toExhaust)
        {
            if (card.Pile is { } pile && BookLibraryUtility.IsBookLibraryPile(pile.Type))
            {
                await CardPileCmd.Add(card, PileType.Discard);
            }
        }

        var hitCount = toExhaust.Count;
        if (hitCount == 0)
        {
            return;
        }

        await KnowledgeDemon.WithKnowledgeDemonAttackAnim(
            DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .WithHitCount(hitCount)
                .FromCard(this)
                .Targeting(cardPlay.Target),
            Owner.Character,
            onlyPlayAnimOnce: true)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}
