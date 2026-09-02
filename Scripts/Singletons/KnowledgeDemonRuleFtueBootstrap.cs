using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;
using STS2RitsuLib;

namespace ComicChess.KnowledgeDemon;

public static class KnowledgeDemonRuleFtueBootstrap
{
    private static bool _registered;
    private static IDisposable? _combatStartingSubscription;
    private static IDisposable? _cardGeneratedSubscription;
    private static readonly SemaphoreSlim _ftueGate = new(1, 1);

    public static void Register()
    {
        if (_registered)
        {
            return;
        }

        _registered = true;
        Entry.Logger.Info("[KnowledgeDemon] Registering first-combat FTUE lifecycle subscription.");
        _combatStartingSubscription = RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(OnCombatStarting);
        _cardGeneratedSubscription = RitsuLibFramework.SubscribeLifecycle<CardGeneratedForCombatEvent>(OnCardGeneratedForCombat);
    }

    private static void OnCombatStarting(CombatStartingEvent evt)
    {
        Entry.Logger.Info("[KnowledgeDemon] CombatStartingEvent received for FTUE check.");
        _ = ShowFtueAsync(evt);
    }

    private static void OnCardGeneratedForCombat(CardGeneratedForCombatEvent evt)
    {
        if (evt.Card is not Infinite || evt.Card.Owner is not { Character: KnowledgeDemon } player)
        {
            return;
        }

        Entry.Logger.Info("[KnowledgeDemon] Infinite card generated; queuing Infinite rules FTUE.");
        _ = QueueRuleFtueAsync(NKnowledgeDemonRuleFtue.RuleType.Infinity);
    }

    public static void NotifyInfiniteGenerated(Player player)
    {
        if (player.Character is not KnowledgeDemon)
        {
            return;
        }

        Entry.Logger.Info("[KnowledgeDemon] Infinite card created by transformation; queuing Infinite rules FTUE.");
        _ = QueueRuleFtueAsync(NKnowledgeDemonRuleFtue.RuleType.Infinity);
    }

    private static async Task ShowFtueAsync(CombatStartingEvent evt)
    {
        if (TestMode.IsOn || evt.CombatState is null)
        {
            Entry.Logger.Info("[KnowledgeDemon] FTUE skipped because test mode is on or combat state was not ready.");
            return;
        }

        var player = evt.RunState.Players.FirstOrDefault(static candidate => candidate.Character is KnowledgeDemon);
        if (player is null)
        {
            Entry.Logger.Info("[KnowledgeDemon] FTUE skipped because this run has no Knowledge Demon player.");
            return;
        }

        await _ftueGate.WaitAsync();
        try
        {
            if (!HasCompletedRuleFtue(NKnowledgeDemonRuleFtue.id))
            {
                await ShowRuleFtueAsync(NKnowledgeDemonRuleFtue.RuleType.KnowledgeDemon);
            }

            if (!HasCompletedRuleFtue(NKnowledgeDemonRuleFtue.UniqueId)
                && PileType.Deck.GetPile(player).Cards.Any(KnowledgeDemonUniqueUtility.IsUnique))
            {
                await ShowRuleFtueAsync(NKnowledgeDemonRuleFtue.RuleType.Unique);
            }
        }
        finally
        {
            _ftueGate.Release();
        }
    }

    private static async Task QueueRuleFtueAsync(NKnowledgeDemonRuleFtue.RuleType ruleType)
    {
        await _ftueGate.WaitAsync();
        try
        {
            await ShowRuleFtueAsync(ruleType);
        }
        finally
        {
            _ftueGate.Release();
        }
    }

    private static async Task ShowRuleFtueAsync(NKnowledgeDemonRuleFtue.RuleType ruleType)
    {
        var tutorialId = ruleType switch
        {
            NKnowledgeDemonRuleFtue.RuleType.Unique => NKnowledgeDemonRuleFtue.UniqueId,
            NKnowledgeDemonRuleFtue.RuleType.Infinity => NKnowledgeDemonRuleFtue.InfinityId,
            _ => NKnowledgeDemonRuleFtue.id,
        };

        if (TestMode.IsOn || HasCompletedRuleFtue(tutorialId))
        {
            return;
        }

        for (var i = 0; i < 20 && NModalContainer.Instance is null; i++)
        {
            await Cmd.CustomScaledWait(0.1f, 0.25f);
        }

        if (NModalContainer.Instance is null)
        {
            Entry.Logger.Warn($"[KnowledgeDemon] {tutorialId} FTUE skipped because the modal container never became ready.");
            return;
        }

        var ftue = NKnowledgeDemonRuleFtue.Create(ruleType);
        if (ftue is null)
        {
            Entry.Logger.Warn($"[KnowledgeDemon] {tutorialId} FTUE skipped because the scene could not be created.");
            return;
        }

        Entry.Logger.Info($"[KnowledgeDemon] Showing {tutorialId} FTUE.");
        SaveManager.Instance.MarkFtueAsComplete(tutorialId);
        NModalContainer.Instance.Add(ftue, showBackstop: false);
        await Cmd.CustomScaledWait(0.5f, 1f);
        ftue.Start();
        await ftue.WaitForCompletionAsync();
    }

    private static bool HasCompletedRuleFtue(string tutorialId) =>
        SaveManager.Instance.Progress.FtueCompleted.Contains(tutorialId);
}
