using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class HiveConsciousness : KnowledgeDemonCardModel
{
    private const int energyCost = 1;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Rare;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Sly];

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(5m, ValueProp.Move)];

    public HiveConsciousness()
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _ = choiceContext;

        if (Pile is not { } listenerPile || !BookLibraryUtility.IsBookLibraryPile(listenerPile.Type))
        {
            return;
        }

        if (cardPlay.Card.Owner != Owner)
        {
            return;
        }

        var libraryPile = BookLibraryUtility.TryGetLibraryPile(Owner);
        if (libraryPile is null || libraryPile.Cards.Count == 0)
        {
            return;
        }

        foreach (var libraryCard in libraryPile.Cards.ToList())
        {
            if (libraryCard is HiveConsciousness hive && hive.IsUpgraded == IsUpgraded)
            {
                continue;
            }

            await BookLibraryUtility.TransformCard(libraryCard, CreateClone());
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}
