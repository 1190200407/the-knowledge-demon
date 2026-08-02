using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Cards.Transforms;

namespace ComicChess.KnowledgeDemon;

/// <summary>知识恶魔 mod 事件 hook 分发。</summary>
public static class KnowledgeDemonHook
{
    public static async Task<bool> CanRecordToLibrary(
        PlayerChoiceContext? choiceContext,
        Player player,
        CardModel sourceCard)
    {
        var combatState = player.Creature.CombatState;
        if (combatState is null)
        {
            return true;
        }

        foreach (var model in combatState.IterateHookListeners())
        {
            if (model is not IKnowledgeDemonEventListener listener)
            {
                continue;
            }

            if (!await listener.CanRecordToLibrary(choiceContext, player, sourceCard))
            {
                return false;
            }
        }

        return true;
    }

    public static async Task BeforeRecordedToLibrary(
        PlayerChoiceContext? choiceContext,
        Player player,
        CardModel sourceCard)
    {
        var combatState = player.Creature.CombatState;
        if (combatState is null)
        {
            return;
        }

        foreach (var model in combatState.IterateHookListeners())
        {
            if (model is not IKnowledgeDemonEventListener listener)
            {
                continue;
            }

            await listener.BeforeRecordedToLibrary(choiceContext, player, sourceCard);
        }
    }

    public static async Task<CardModel> ModifyRecordCard(Player player, CardModel sourceCard)
    {
        var recordTemplate = sourceCard;
        var combatState = player.Creature.CombatState;
        if (combatState is null)
        {
            return recordTemplate;
        }

        foreach (var model in combatState.IterateHookListeners())
        {
            if (model is not IKnowledgeDemonEventListener listener)
            {
                continue;
            }

            recordTemplate = await listener.ModifyRecordCard(player, sourceCard, recordTemplate);
        }

        return recordTemplate;
    }

    public static async Task<CardModel> ModifyRecordCardLate(
        Player player,
        CardModel sourceCard,
        CardModel recordTemplate)
    {
        var combatState = player.Creature.CombatState;
        if (combatState is null)
        {
            return recordTemplate;
        }

        foreach (var model in combatState.IterateHookListeners())
        {
            if (model is not IKnowledgeDemonEventListener listener)
            {
                continue;
            }

            recordTemplate = await listener.ModifyRecordCardLate(player, sourceCard, recordTemplate);
        }

        return recordTemplate;
    }

    public static async Task AfterRecordedToLibrary(
        PlayerChoiceContext? choiceContext,
        Player player,
        CardModel sourceCard,
        IReadOnlyList<CardModel> recordedCopies)
    {
        var combatState = player.Creature.CombatState;
        if (combatState is null)
        {
            return;
        }

        foreach (var model in combatState.IterateHookListeners())
        {
            if (model is not IKnowledgeDemonEventListener listener)
            {
                continue;
            }

            if (recordedCopies.Contains(model))
            {
                continue;
            }

            await listener.AfterRecordedToLibrary(choiceContext, player, sourceCard, recordedCopies);
        }
    }

    public static async Task BeforeChooseFromLibrary(
        PlayerChoiceContext choiceContext,
        Player player,
        CardModel? chooseSource,
        IReadOnlyList<CardModel> candidates)
    {
        var combatState = player.Creature.CombatState;
        if (combatState is null)
        {
            return;
        }

        foreach (var model in combatState.IterateHookListeners())
        {
            if (model is not IKnowledgeDemonEventListener listener)
            {
                continue;
            }

            await listener.BeforeChooseFromLibrary(choiceContext, player, chooseSource, candidates);
        }
    }

    public static async Task AfterChooseFromLibrary(
        PlayerChoiceContext choiceContext,
        Player player,
        CardModel? chooseSource,
        CardModel? chosen,
        IReadOnlyList<CardModel> candidates)
    {
        var combatState = player.Creature.CombatState;
        if (combatState is null)
        {
            return;
        }

        foreach (var model in combatState.IterateHookListeners())
        {
            if (model is not IKnowledgeDemonEventListener listener)
            {
                continue;
            }

            await listener.AfterChooseFromLibrary(
                choiceContext,
                player,
                chooseSource,
                chosen,
                candidates);
        }
    }

    public static async Task<CardModel> ModifyMaterializeCard(
        Player player,
        CardModel sourceCard,
        CardModel materializedCard)
    {
        var result = materializedCard;
        var combatState = player.Creature.CombatState;
        if (combatState is null)
        {
            return result;
        }

        foreach (var model in combatState.IterateHookListeners())
        {
            if (model is not IKnowledgeDemonEventListener listener)
            {
                continue;
            }

            result = await listener.ModifyMaterializeCard(player, sourceCard, result);
        }

        return result;
    }

    public static async Task AfterMaterializedFromLibrary(
        PlayerChoiceContext choiceContext,
        Player player,
        IReadOnlyList<CardModel> materialized)
    {
        var combatState = player.Creature.CombatState;
        if (combatState is null)
        {
            return;
        }

        foreach (var model in combatState.IterateHookListeners())
        {
            if (model is not IKnowledgeDemonEventListener listener)
            {
                continue;
            }

            await listener.AfterMaterializedFromLibrary(choiceContext, player, materialized);
        }
    }

    public static async Task AfterCardTransformed(ModCardTransformContext context)
    {
        var player = context.Replacement.Owner;
        var combatState = player.Creature.CombatState;
        if (combatState is null)
        {
            return;
        }

        foreach (var model in combatState.IterateHookListeners())
        {
            if (model is not IKnowledgeDemonEventListener listener)
            {
                continue;
            }

            await listener.AfterCardTransformed(player, context);
        }
    }

    public static async Task AfterKnowledgeOverloadTriggered(
        PlayerChoiceContext choiceContext,
        Player player,
        AbstractModel? source)
    {
        var combatState = player.Creature.CombatState;
        if (combatState is null)
        {
            return;
        }

        foreach (var model in combatState.IterateHookListeners())
        {
            if (model is not IKnowledgeDemonEventListener listener)
            {
                continue;
            }

            await listener.AfterKnowledgeOverloadTriggered(choiceContext, player, source);
        }
    }
}

/// <summary>知识恶魔 mod 事件监听；未覆写的方法使用默认空实现。</summary>
public interface IKnowledgeDemonEventListener
{
    Task<bool> CanRecordToLibrary(
        PlayerChoiceContext? choiceContext,
        Player player,
        CardModel sourceCard) => Task.FromResult(true);

    Task BeforeRecordedToLibrary(
        PlayerChoiceContext? choiceContext,
        Player player,
        CardModel sourceCard) => Task.CompletedTask;

    Task<CardModel> ModifyRecordCard(Player player, CardModel sourceCard, CardModel recordTemplate) =>
        Task.FromResult(recordTemplate);

    Task<CardModel> ModifyRecordCardLate(Player player, CardModel sourceCard, CardModel recordTemplate) =>
        Task.FromResult(recordTemplate);

    Task AfterRecordedToLibrary(
        PlayerChoiceContext? choiceContext,
        Player player,
        CardModel sourceCard,
        IReadOnlyList<CardModel> recordedCopies) => Task.CompletedTask;

    Task BeforeChooseFromLibrary(
        PlayerChoiceContext choiceContext,
        Player player,
        CardModel? chooseSource,
        IReadOnlyList<CardModel> candidates) => Task.CompletedTask;

    Task AfterChooseFromLibrary(
        PlayerChoiceContext choiceContext,
        Player player,
        CardModel? chooseSource,
        CardModel? chosen,
        IReadOnlyList<CardModel> candidates) => Task.CompletedTask;

    Task<CardModel> ModifyMaterializeCard(
        Player player,
        CardModel sourceCard,
        CardModel materializedCard) => Task.FromResult(materializedCard);

    Task AfterMaterializedFromLibrary(
        PlayerChoiceContext choiceContext,
        Player player,
        IReadOnlyList<CardModel> materialized) => Task.CompletedTask;

    Task AfterCardTransformed(
        Player player,
        ModCardTransformContext context) => Task.CompletedTask;

    Task AfterKnowledgeOverloadTriggered(
        PlayerChoiceContext choiceContext,
        Player player,
        AbstractModel? source) => Task.CompletedTask;
}
