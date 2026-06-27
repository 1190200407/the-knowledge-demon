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

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class HiveConsciousness : KnowledgeDemonCardModel
{
    private const int energyCost = 1;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Uncommon;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Sly];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(5m, ValueProp.Move)];

    public HiveConsciousness()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(choiceContext);

        var libraryPile = BookLibraryUtility.TryGetLibraryPile(Owner);
        if (libraryPile is null || libraryPile.Cards.Count == 0 || CardScope is null)
        {
            return;
        }

        foreach (var libraryCard in libraryPile.Cards.ToList())
        {
            if (libraryCard is HiveConsciousness hive && hive.IsUpgraded == IsUpgraded)
            {
                continue;
            }

            var replacement = CardScope.CreateCard<HiveConsciousness>(Owner);
            if (IsUpgraded && !replacement.IsUpgraded)
            {
                CardCmd.Upgrade(replacement, MegaCrit.Sts2.Core.Nodes.CommonUi.CardPreviewStyle.None);
            }

            await BookLibraryUtility.TransformCard(libraryCard, replacement);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}
