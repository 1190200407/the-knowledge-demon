using System.Reflection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace ComicChess.KnowledgeDemon;

public static class KnowledgeDemonHistory
{
    private const BindingFlags InstanceAny = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly MethodInfo AddMethod =
        typeof(CombatHistory).GetMethod("Add", InstanceAny)!;

    public static void LogChoose(Player player, CardModel? sourceCard, CardModel chosenCard)
    {
        var combatState = player.Creature.CombatState;
        var history = CombatManager.Instance?.History;
        if (combatState is null || history is null)
        {
            return;
        }

        var entry = new KnowledgeDemonChooseEntry(
            player,
            sourceCard,
            chosenCard,
            combatState.RoundNumber,
            combatState.CurrentSide,
            history,
            combatState.Players);

        AddMethod.Invoke(history, [combatState, entry]);
    }
}
