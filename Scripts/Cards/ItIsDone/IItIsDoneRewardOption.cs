using System.Threading.Tasks;

namespace ComicChess.KnowledgeDemon;

/// <summary>此事已成奖励选项卡：在选牌界面被选中后立即执行效果。</summary>
public interface IItIsDoneRewardOption
{
    Task OnChosen();
}
