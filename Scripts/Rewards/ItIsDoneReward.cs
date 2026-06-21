using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Combat.Rewards;

namespace ComicChess.KnowledgeDemon;

internal abstract class ItIsDoneRewardBase : ModCustomReward
{
    internal const string CharacterIconPath = "res://KnowledgeDemon/images/charui/knowledge_demon_boss.png";

    protected ItIsDoneRewardBase(Player player) : base(player)
    {
    }

    protected override string? RewardIconPath => CharacterIconPath;

    protected override string DescriptionLocTable => "gameplay_ui";

    public override void MarkContentAsSeen()
    {
    }

    protected override async Task<bool> OnSelect()
    {
        var chosen = await CardSelectCmd.FromChooseACardScreen(
            new BlockingPlayerChoiceContext(),
            CreateOptionCards(),
            Player,
            canSkip: false);

        if (chosen is IItIsDoneRewardOption option)
        {
            await option.OnChosen();
            return true;
        }

        return false;
    }

    protected abstract List<CardModel> CreateOptionCards();
}

internal sealed class ItIsDoneReward : ItIsDoneRewardBase
{
    public ItIsDoneReward(Player player) : base(player)
    {
    }

    public override RewardType ModRewardType => ItIsDoneRewardRegistration.BaseRewardType;

    protected override string DescriptionLocKey => "KNOWLEDGE_DEMON_REWARD_IT_IS_DONE";

    protected override List<CardModel> CreateOptionCards() =>
        ItIsDoneRewardOptions.Create(Player, upgraded: false);
}

internal sealed class ItIsDoneUpgradedReward : ItIsDoneRewardBase
{
    public ItIsDoneUpgradedReward(Player player) : base(player)
    {
    }

    public override RewardType ModRewardType => ItIsDoneRewardRegistration.UpgradedRewardType;

    protected override string DescriptionLocKey => "KNOWLEDGE_DEMON_REWARD_IT_IS_DONE_UPGRADED";

    protected override List<CardModel> CreateOptionCards() =>
        ItIsDoneRewardOptions.Create(Player, upgraded: true);
}

internal static class ItIsDoneRewardRegistration
{
    internal const string BaseRewardStem = "IT_IS_DONE";
    internal const string UpgradedRewardStem = "IT_IS_DONE_UPGRADED";

    internal static RewardType BaseRewardType { get; private set; }
    internal static RewardType UpgradedRewardType { get; private set; }

    internal static void Register()
    {
        var registry = ModRewardRegistry.For(Entry.ModId);
        BaseRewardType = registry.RegisterOwned(BaseRewardStem, CreateBaseFromSave).RewardType;
        UpgradedRewardType = registry.RegisterOwned(UpgradedRewardStem, CreateUpgradedFromSave).RewardType;
    }

    private static Reward CreateBaseFromSave(SerializableReward save, Player player, string? json) =>
        new ItIsDoneReward(player);

    private static Reward CreateUpgradedFromSave(SerializableReward save, Player player, string? json) =>
        new ItIsDoneUpgradedReward(player);
}
