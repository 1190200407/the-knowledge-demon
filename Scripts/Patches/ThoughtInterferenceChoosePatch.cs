using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils.HarmonyIl;

namespace ComicChess.KnowledgeDemon;

internal sealed class ThoughtInterferenceChoosePatch : IPatchMethod
{
    public static string PatchId => "knowledgedemon_thought_interference_choose_replay";
    public static string Description => "Grant replay to the first choices each turn for Thought Interference";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(CardSelectCmd), nameof(CardSelectCmd.FromChooseACardScreen), [typeof(PlayerChoiceContext), typeof(IReadOnlyList<CardModel>), typeof(Player), typeof(bool)]),
    ];

    public static void Postfix(
        PlayerChoiceContext context,
        IReadOnlyList<CardModel> cards,
        Player player,
        bool canSkip,
        ref Task<CardModel?> __result)
    {
        __result = HarmonyAsyncTaskBridge.After(
            __result,
            async chosen =>
            {
                if (chosen is null)
                {
                    return;
                }

                var power = chosen.Owner.Creature.GetPower<ThoughtInterferencePower>();
                if (power is null)
                {
                    return;
                }

                await power.TryGrantReplayForChooseACard(context, chosen);
            });
    }
}
