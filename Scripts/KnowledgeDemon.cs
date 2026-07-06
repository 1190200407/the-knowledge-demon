using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Data.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Godot;

namespace ComicChess.KnowledgeDemon;

[RegisterCharacter]
public class KnowledgeDemon : ModCharacterTemplate<KnowledgeDemonCardPool, KnowledgeDemonRelicPool, KnowledgeDemonPotionPool>
{
    // 角色名称颜色 rgb(135, 97, 49)  
    public override Color NameColor => new(135f / 255f, 97f / 255f, 49f / 255f);
    // 能量图标轮廓颜色 rgb(166, 115, 56)
    public override Color EnergyLabelOutlineColor => new(135f / 255f, 97f / 255f, 49f / 255f);
    // 地图绘制颜色 rgb(135, 97, 49)
    public override Color MapDrawingColor => new(135f / 255f, 97f / 255f, 49f / 255f);

    // 人物性别
    public override CharacterGender Gender => CharacterGender.Masculine;

    // 初始血量和金币
    public override int StartingHp => 80;
    public override int StartingGold => 99;

    public override string CustomVisualsPath => "res://KnowledgeDemon/scenes/knowledge_demon.tscn";

    public override CharacterAssetProfile AssetProfile => CharacterAssetProfiles.Merge(
        CharacterAssetProfiles.Regent(),
        new(
            Scenes: new(
                VisualsPath: CustomVisualsPath
                // 能量表盘tscn路径。
                //EnergyCounterPath: "res://Test/scenes/test_energy_counter.tscn",
                // 商店人物场景。
                //MerchantAnimPath: "res://Test/scenes/test_character_merchant.tscn",
                // 篝火休息场景。
                //RestSiteAnimPath: "res://Test/scenes/test_character_rest_site.tscn"
            ),
            Ui: new(
                // 对于图片，只要是godot支持的格式都可以，例如png,jpg,svg等等，之后不再说明
                // 人物头像路径。自适应大小。
                IconTexturePath: "res://KnowledgeDemon/images/charui/knowledge_demon_boss.png",
                IconOutlineTexturePath: "res://KnowledgeDemon/images/charui/knowledge_demon_boss_outline.png",
                // 游戏左上角头像、角色统计页头像、每日挑战角色头像。这个是场景而不是图片。参考下方附赠资源搭建。
                IconPath: "res://KnowledgeDemon/scenes/knowledge_demon_icon.tscn",
                // 人物选择背景。
                CharacterSelectBgPath: "res://KnowledgeDemon/scenes/char_select_bg_knowledge_demon.tscn"
                // 人物选择图标。
                //CharacterSelectIconPath: "res://Test/images/char_select_test.png",
                // 人物选择图标-锁定状态。
                //CharacterSelectLockedIconPath: "res://Test/images/char_select_test_locked.png",
                // 人物选择过渡动画。
                // CharacterSelectTransitionPath: "res://materials/transitions/ironclad_transition_mat.tres",
                // 地图上的角色标记图标、表情轮盘上的角色头像。
                //MapMarkerPath: "res://KnowledgeDemon/images/charui/knowledge_demon_boss.png"
            ),
            Vfx: new(
                // 卡牌拖尾场景。
                // TrailPath: "res://scenes/vfx/card_trail_ironclad.tscn"
            ),
            Audio: new(
                // MonsterModel 默认 attack / cast / die
                AttackSfx: "event:/sfx/enemy/enemy_attacks/knowledge_demon/knowledge_demon_attack",
                CastSfx: "event:/sfx/enemy/enemy_attacks/knowledge_demon/knowledge_demon_cast",
                DeathSfx: "event:/sfx/enemy/enemy_attacks/knowledge_demon/knowledge_demon_die",
                CharacterSelectSfx: ClapSfx,
                CharacterTransitionSfx: FlameSfx
            ),
            Multiplayer: new(
                // 多人模式-手指。
                // ArmPointingTexturePath: null,
                // 多人模式剪刀石头布-石头。
                // ArmRockTexturePath: null,
                // 多人模式剪刀石头布-布。
                // ArmPaperTexturePath: null,
                // 多人模式剪刀石头布-剪刀。
                // ArmScissorsTexturePath: null
            )
            // 其余如果有需要自行取消注释使用
            // Spine: null,
            // VisualCues: null, // 帧动画静态图人物使用，查看角色动画一章
            // WorldProceduralVisuals: null,
            // 以下为让遗物根据你的人物展现不同的图像资源，在列表里添加即可
            // VanillaCardVisualOverrides: [],
            // VanillaRelicVisualOverrides: [
            //     new (CharacterOwnedVanillaRelicModelId.YummyCookie, new("res://icon.svg")) // 美味饼干覆盖
            // ],
            // VanillaPotionVisualOverrides: []
        ));

    // 对齐原版 Boss：Slap MediumAttackTrigger 0.5f；诅咒 MindRotTrigger 1f
    public override float AttackAnimDelay => 0.5f;
    public override float CastAnimDelay => 1f;

    public override bool RequiresEpochAndTimeline => false;

    // 自动转换人物场景，让你不需要手动挂脚本。复制即可。
    protected override NCreatureVisuals? TryCreateCreatureVisuals() =>
        RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(CustomVisualsPath);

    /// <summary>
    /// 标准玩家 trigger → Boss 骨架动画名：
    /// Idle <c>idle_loop</c>；Hit <c>hurt</c>；Attack <c>attack_light</c>；Cast <c>brain_rot</c>；
    /// heavyAttack <c>attack_medium</c>；superAttack <c>attack_heavy</c>；Dead <c>die</c>。
    /// </summary>
    protected override CreatureAnimator? SetupCustomCreatureAnimator(MegaSprite controller)
    {
        var idle = new AnimState("idle_loop", isLooping: true);
        var cast = new AnimState("brain_rot");
        var attack = new AnimState("attack_light");
        var hurt = new AnimState("hurt");
        var dead = new AnimState("die");
        var heavyAttack = new AnimState("attack_medium");
        var superAttack = new AnimState("attack_heavy");
        cast.NextState = idle;
        attack.NextState = idle;
        hurt.NextState = idle;
        heavyAttack.NextState = idle;
        superAttack.NextState = idle;

        var animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Idle", idle);
        animator.AddAnyState("Dead", dead);
        animator.AddAnyState("Hit", hurt);
        animator.AddAnyState("Attack", attack);
        animator.AddAnyState("Cast", cast);
        animator.AddAnyState("heavyAttack", heavyAttack);
        animator.AddAnyState("superAttack", superAttack);
        return animator;
    }

    public static string GetHeavyAnimIfApplicable(CharacterModel character)
    {
        if (character is KnowledgeDemon)
        {
            return "heavyAttack";
        }

        return Ironclad.GetHeavyAnimIfApplicable(character);
    }

    /// <summary>原版 Slap <c>MediumAttackTrigger</c> 对齐延迟。</summary>
    public static float GetHeavyAttackDelayIfApplicable(CharacterModel character)
    {
        if (character is KnowledgeDemon)
        {
            return 0.5f;
        }

        return Ironclad.GetHeavyAttackDelayIfApplicable(character);
    }

    public static string GetSuperAnimIfApplicable(CharacterModel character) =>
        character is KnowledgeDemon ? "superAttack" : "Attack";

    /// <summary>原版 Knowledge Overwhelming <c>superAttack</c> 对齐延迟。</summary>
    public static float GetSuperAttackDelayIfApplicable(CharacterModel character) =>
        character is KnowledgeDemon ? 0.85f : 0f;

    /// <summary>群攻 / 多段：知识恶魔用 heavyAttack；多段时仅播一次攻击动画。</summary>
    public static AttackCommand WithKnowledgeDemonAttackAnim(
        AttackCommand cmd,
        CharacterModel character,
        bool onlyPlayAnimOnce = false)
    {
        cmd = cmd.WithAttackerAnim(
            GetHeavyAnimIfApplicable(character),
            GetHeavyAttackDelayIfApplicable(character));
        return onlyPlayAnimOnce ? cmd.OnlyPlayAnimOnce() : cmd;
    }

    /// <summary>原版 Slap <c>knowledge_demon_slap</c>。</summary>
    public const string SlapSfx = "event:/sfx/enemy/enemy_attacks/knowledge_demon/knowledge_demon_slap";

    /// <summary>原版 Knowledge Overwhelming <c>knowledge_demon_clap</c>。</summary>
    public const string ClapSfx = "event:/sfx/enemy/enemy_attacks/knowledge_demon/knowledge_demon_clap";

    /// <summary>原版定义、未在 C# 招式里播放；灼烧/火焰类效果可用。</summary>
    public const string FlameSfx = "event:/sfx/enemy/enemy_attacks/knowledge_demon/knowledge_demon_flame";

    /// <summary>MonsterModel 默认攻击音（未在 Boss 招式里单独使用）。</summary>
    public const string DefaultAttackSfx = "event:/sfx/enemy/enemy_attacks/knowledge_demon/knowledge_demon_attack";

    // 初始卡组，或者在卡牌类上用RegisterCharacterStarterCard就不用写这个
    // protected override IEnumerable<StartingDeckEntry> StartingDeckEntries => [
    //     new(typeof(TestCard), 5)
    // ];

    // 初始遗物，或者在遗物类上用RegisterCharacterStarterRelic就不用写这个
    // protected override IEnumerable<Type> StartingRelicTypes => [
    //     typeof(Akabeko)
    // ];

    // 攻击建筑师的攻击特效列表
    public override List<string> GetArchitectAttackVfx() => [
        "vfx/vfx_attack_blunt",
        "vfx/vfx_heavy_blunt",
        "vfx/vfx_attack_slash",
        "vfx/vfx_bloody_impact",
        "vfx/vfx_rock_shatter"
    ];
}