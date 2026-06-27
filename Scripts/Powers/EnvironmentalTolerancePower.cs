using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterPower]
public sealed class EnvironmentalTolerancePower : KnowledgeDemonPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<string> RegisteredKeywordIds =>
        [KnowledgeDemonKeyword.Unique];

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _ = applier;
        _ = cardSource;

        if (Owner.Player is not { } player || KnowledgeDemonUniqueSingleton.Instance is null)
        {
            return;
        }

        await KnowledgeDemonUniqueSingleton.Instance.AddUniqueKeywordsAndResolveAsync(
            player,
            KnowledgeDemonUniqueUtility.IterateCombatUniqueScope(player)
                .Where(card => card.Type == CardType.Status));
    }
}
