using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class Infinite : KnowledgeDemonCardModel
{
    private const int EnergyCostValue = 0;
    private const CardType TypeValue = CardType.Status;
    private const CardRarity RarityValue = CardRarity.Ancient;
    private const TargetType TargetTypeValue = TargetType.Self;
    private const bool ShouldShowInCardLibraryValue = false;

    public override bool CanBeGeneratedInCombat => false;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Sly];

    public Infinite()
        : base(EnergyCostValue, TypeValue, RarityValue, TargetTypeValue, ShouldShowInCardLibraryValue)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<InfinitePower>(
            choiceContext,
            Owner.Creature,
            1m,
            Owner.Creature,
            this);
    }
}
