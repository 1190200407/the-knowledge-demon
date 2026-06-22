using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;

namespace ComicChess.KnowledgeDemon;

internal static class EnlightenmentAttainedOptionHoverTips
{
    internal static IEnumerable<IHoverTip> All(bool upgraded) => [
        HoverTipFactory.FromCard<BloodSacrificeOption>(upgrade: upgraded),
        HoverTipFactory.FromCard<WealthGatheringOption>(upgrade: upgraded),
        HoverTipFactory.FromCard<MetamorphosisOption>(upgrade: upgraded),
    ];
}

internal static class EnlightenmentAttainedRewardOptions
{
    internal static List<CardModel> Create(Player player, bool upgraded)
    {
        var runState = player.RunState;
        var blood = runState.CreateCard<BloodSacrificeOption>(player);
        var wealth = runState.CreateCard<WealthGatheringOption>(player);
        var metamorphosis = runState.CreateCard<MetamorphosisOption>(player);

        if (upgraded)
        {
            CardCmd.Upgrade([blood, wealth, metamorphosis], CardPreviewStyle.None);
        }

        return [blood, wealth, metamorphosis];
    }
}
