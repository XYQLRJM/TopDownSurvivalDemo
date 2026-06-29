using UnityEngine;

/// <summary>
/// 协调关卡开始、结束、存档快照和关后流程。
/// </summary>
public class GameLevelManager : MonoBehaviour
{
    /// <summary>当前游戏总关卡数量。</summary>
    private const int MaxStage = 10;
    /// <summary>每一关的持续时间，数组下标对应关卡减一。</summary>
    private static readonly int[] StageDurations = { 45, 45, 45, 45, 60, 45, 45, 45, 45, 60 };

    /// <summary>当前正在游戏中的玩家。</summary>
    private PlayerController2D player;
    /// <summary>当前玩家的等级、经验和金币数据。</summary>
    private PlayerProgression progression;
    /// <summary>当前关卡使用的小怪刷怪器。</summary>
    private MonsterSpawner monsterSpawner;
    /// <summary>游戏主界面面板。</summary>
    private GamePanel gamePanel;
    /// <summary>当前关卡倒计时器。</summary>
    private GameStageTimer stageTimer;
    /// <summary>关后奖励、强化和商店流程。</summary>
    private GameRewardFlow rewardFlow;
    /// <summary>Boss 关卡生成和绑定服务。</summary>
    private GameBossStageService bossStageService;
    /// <summary>当前关卡中的 Boss 引用。</summary>
    private BossController currentBoss;
    /// <summary>当前关卡编号。</summary>
    private int currentStage = 1;
    /// <summary>关卡开始时的玩家等级，用于计算本关升级次数。</summary>
    private int stageStartLevel;
    /// <summary>当前关卡是否已经结算。</summary>
    private bool stageEnded;

    /// <summary>初始化关卡管理器依赖，并从指定关卡开始。</summary>
    public void Init(PlayerController2D currentPlayer, PlayerProgression playerProgression, MonsterSpawner spawner, GamePanel panel, int startStage = 1)
    {
        player = currentPlayer;
        progression = playerProgression;
        monsterSpawner = spawner;
        gamePanel = panel;
        currentStage = Mathf.Clamp(startStage, 1, MaxStage);
        stageTimer = new GameStageTimer();
        rewardFlow = new GameRewardFlow(this, player, progression);
        bossStageService = new GameBossStageService(player, monsterSpawner, gamePanel, OnBossDead);
        StartStage();
    }

    /// <summary>玩家死亡时触发失败结算。</summary>
    public void OnPlayerDead(PlayerController2D deadPlayer)
    {
        if (player == deadPlayer)
            player = null;

        EndStage(false);
    }

    /// <summary>保存当前关卡开局状态，继续游戏时会从该快照恢复。</summary>
    public void SaveCurrentStageSnapshot()
    {
        if (player == null || progression == null)
            return;

        PlayerRelicEffects relicEffects = player.GetComponent<PlayerRelicEffects>();
        GameSaveStore.Save(new GameSaveData
        {
            characterId = player.RuntimeData.Id,
            stage = currentStage,
            runtimeData = player.RuntimeData.CaptureSaveData(),
            progressionData = progression.CaptureSaveData(),
            relicData = relicEffects != null ? relicEffects.CaptureSaveData() : null
        });
    }

    /// <summary>销毁关卡管理器时停止计时并恢复时间缩放。</summary>
    private void OnDestroy()
    {
        stageTimer?.Stop();
        rewardFlow?.Dispose();
        Time.timeScale = 1f;
    }

    /// <summary>启动当前关卡，刷新 UI、回血、刷怪并保存开局快照。</summary>
    private void StartStage()
    {
        int stageDuration = GetStageDurationSeconds();
        stageEnded = false;
        stageStartLevel = progression != null ? progression.Level : 0;
        player?.RuntimeData.HealFull();

        gamePanel?.SetStageText(currentStage, MaxStage);
        gamePanel?.SetRemainingTime(stageDuration);
        Time.timeScale = 1f;

        if (GameBossStageService.IsBossStage(currentStage))
            bossStageService.StartBossStage(currentStage);
        else
            StartNormalStage();

        SaveCurrentStageSnapshot();
        stageTimer.Start(stageDuration, seconds => gamePanel?.SetRemainingTime(seconds), OnStageTimeOver);
    }

    /// <summary>启动普通关卡刷怪逻辑。</summary>
    private void StartNormalStage()
    {
        currentBoss = null;
        bossStageService.ClearBoss();
        monsterSpawner?.SetStage(currentStage);
        monsterSpawner?.SetSpawning(true);
    }

    /// <summary>关卡倒计时结束后，根据玩家是否存活进行结算。</summary>
    private void OnStageTimeOver()
    {
        if (stageEnded)
            return;

        bool isPlayerAlive = player != null && player.RuntimeData.CurrentHp > 0;
        EndStage(isPlayerAlive, false);
    }

    /// <summary>结束当前关卡并打开结算面板。</summary>
    private void EndStage(bool isWin, bool removeTimer = true)
    {
        if (stageEnded)
            return;

        stageEnded = true;
        if (removeTimer)
            stageTimer.Stop();

        monsterSpawner?.SetSpawning(false);
        Time.timeScale = 0f;
        UIMgr.Instance.ShowPanel<ResultPanel>(E_UILayer.Top, panel =>
        {
            if (!isWin)
                panel.SetResultText("游戏失败", ReturnToMenuAndClearSave);
            else if (currentStage >= MaxStage)
                panel.SetResultText("游戏胜利", ReturnToMenuAndClearSave);
            else
                panel.SetResultText("成功存活", BeginPostStageFlow);
        }, true);
    }

    /// <summary>开始执行结算后的奖励、强化和商店流程。</summary>
    private void BeginPostStageFlow()
    {
        rewardFlow.Begin(currentStage, stageStartLevel, ProceedToNextStage);
    }

    /// <summary>清理当前关卡并进入下一关。</summary>
    private void ProceedToNextStage()
    {
        currentBoss = bossStageService.CurrentBoss;
        GameStageCleaner.CleanupStage(ref currentBoss, gamePanel);
        bossStageService.ClearBoss();
        currentStage++;
        StartStage();
    }

    /// <summary>返回主菜单并清除当前存档。</summary>
    private void ReturnToMenuAndClearSave()
    {
        GameSaveStore.Clear();
        ReturnToMenu();
    }

    /// <summary>清理场景运行对象并返回开始界面。</summary>
    private void ReturnToMenu()
    {
        currentBoss = bossStageService.CurrentBoss;
        GameStageCleaner.ReturnToMenu(ref currentBoss);
        bossStageService.ClearBoss();
    }

    /// <summary>读取当前关卡对应的倒计时时长。</summary>
    private int GetStageDurationSeconds()
    {
        int index = Mathf.Clamp(currentStage, 1, StageDurations.Length) - 1;
        return StageDurations[index];
    }

    /// <summary>Boss 被击杀时发放奖励并立即通关。</summary>
    private void OnBossDead(BossController boss)
    {
        if (stageEnded || boss != bossStageService.CurrentBoss)
            return;

        progression?.AddLevels(1);
        progression?.AddGold(50);
        currentBoss = null;
        bossStageService.ClearBoss();
        EndStage(true);
    }
}
