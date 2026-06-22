using System.Threading.Tasks;

namespace ComicChess.KnowledgeDemon;

/// <summary>我已悟道奖励选项卡：在选牌界面被选中后立即执行效果。</summary>
public interface IEnlightenmentAttainedRewardOption
{
    Task OnChosen();
}
