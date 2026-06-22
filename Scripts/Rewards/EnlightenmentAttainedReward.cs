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

internal abstract class EnlightenmentAttainedRewardBase : ModCustomReward
{
    internal const string CharacterIconPath = "res://KnowledgeDemon/images/charui/knowledge_demon_boss.png";

    protected EnlightenmentAttainedRewardBase(Player player) : base(player)
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

        if (chosen is IEnlightenmentAttainedRewardOption option)
        {
            await option.OnChosen();
            return true;
        }

        return false;
    }

    protected abstract List<CardModel> CreateOptionCards();
}

internal sealed class EnlightenmentAttainedReward : EnlightenmentAttainedRewardBase
{
    public EnlightenmentAttainedReward(Player player) : base(player)
    {
    }

    public override RewardType ModRewardType => EnlightenmentAttainedRewardRegistration.BaseRewardType;

    protected override string DescriptionLocKey => "KNOWLEDGE_DEMON_REWARD_ENLIGHTENMENT_ATTAINED";

    protected override List<CardModel> CreateOptionCards() =>
        EnlightenmentAttainedRewardOptions.Create(Player, upgraded: false);
}

internal sealed class EnlightenmentAttainedUpgradedReward : EnlightenmentAttainedRewardBase
{
    public EnlightenmentAttainedUpgradedReward(Player player) : base(player)
    {
    }

    public override RewardType ModRewardType => EnlightenmentAttainedRewardRegistration.UpgradedRewardType;

    protected override string DescriptionLocKey => "KNOWLEDGE_DEMON_REWARD_ENLIGHTENMENT_ATTAINED_UPGRADED";

    protected override List<CardModel> CreateOptionCards() =>
        EnlightenmentAttainedRewardOptions.Create(Player, upgraded: true);
}

internal static class EnlightenmentAttainedRewardRegistration
{
    internal const string BaseRewardStem = "ENLIGHTENMENT_ATTAINED";
    internal const string UpgradedRewardStem = "ENLIGHTENMENT_ATTAINED_UPGRADED";

    internal static RewardType BaseRewardType { get; private set; }
    internal static RewardType UpgradedRewardType { get; private set; }

    internal static void Register()
    {
        var registry = ModRewardRegistry.For(Entry.ModId);
        BaseRewardType = registry.RegisterOwned(BaseRewardStem, CreateBaseFromSave).RewardType;
        UpgradedRewardType = registry.RegisterOwned(UpgradedRewardStem, CreateUpgradedFromSave).RewardType;
    }

    private static Reward CreateBaseFromSave(SerializableReward save, Player player, string? json) =>
        new EnlightenmentAttainedReward(player);

    private static Reward CreateUpgradedFromSave(SerializableReward save, Player player, string? json) =>
        new EnlightenmentAttainedUpgradedReward(player);
}
