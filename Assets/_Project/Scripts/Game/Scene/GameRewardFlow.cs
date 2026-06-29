using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 处理关后奖励动画、强化选择和商店进入流程。
/// </summary>
public class GameRewardFlow
{
    /// <summary>奖励动画预制体在 Resources 中的路径。</summary>
    private const string RewardPath = "Prefabs/Items/reward";
    /// <summary>奖励动画相对摄像机中心的 Y 轴偏移。</summary>
    private const float RewardOffsetY = 1.5f;

    /// <summary>用于启动协程的 MonoBehaviour 宿主。</summary>
    private readonly MonoBehaviour runner;
    /// <summary>当前玩家控制器。</summary>
    private readonly PlayerController2D player;
    /// <summary>当前玩家成长数据。</summary>
    private readonly PlayerProgression progression;
    /// <summary>正在播放的奖励动画对象。</summary>
    private GameObject rewardObj;
    /// <summary>待选择的强化次数。</summary>
    private int pendingBuffCount;
    /// <summary>当前完成的关卡编号。</summary>
    private int currentStage;
    /// <summary>关后流程全部结束后的回调。</summary>
    private Action onComplete;

    /// <summary>创建关后奖励流程对象并保存依赖。</summary>
    public GameRewardFlow(MonoBehaviour runner, PlayerController2D player, PlayerProgression progression)
    {
        this.runner = runner;
        this.player = player;
        this.progression = progression;
    }

    /// <summary>根据本关升级次数开始奖励、强化和商店流程。</summary>
    public void Begin(int stage, int stageStartLevel, Action completed)
    {
        onComplete = completed;
        currentStage = stage;
        pendingBuffCount = progression != null ? Mathf.Max(0, progression.Level - stageStartLevel) : 0;
        if (pendingBuffCount > 0)
            runner.StartCoroutine(RewardAndBuffFlow());
        else
            BeginShopOrComplete(stage);
    }

    /// <summary>释放奖励流程中打开的对象和面板。</summary>
    public void Dispose()
    {
        RemoveReward();
        UIMgr.Instance.HidePanel<ChooseBuffPanel>(true);
    }

    /// <summary>先播放奖励动画，再显示强化选择。</summary>
    private IEnumerator RewardAndBuffFlow()
    {
        yield return PlayRewardOnce();
        ShowNextBuffChoice();
    }

    /// <summary>显示下一次强化选择，次数用完后进入后续流程。</summary>
    private void ShowNextBuffChoice()
    {
        if (pendingBuffCount <= 0)
        {
            UIMgr.Instance.HidePanel<ChooseBuffPanel>(true);
            RemoveReward();
            BeginShopOrComplete(currentStage);
            return;
        }

        UIMgr.Instance.ShowPanel<ChooseBuffPanel>(E_UILayer.Top, panel =>
        {
            panel.Bind(player != null ? player.RuntimeData : null, option =>
            {
                if (option == null)
                {
                    pendingBuffCount = 0;
                    ShowNextBuffChoice();
                    return;
                }

                player?.RuntimeData.ApplyBuff(option.type, option.value);
                pendingBuffCount--;
                runner.StartCoroutine(WaitAndShowNextBuff());
            });
        }, true);
    }

    /// <summary>等待短暂时间后继续显示下一次强化。</summary>
    private IEnumerator WaitAndShowNextBuff()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        ShowNextBuffChoice();
    }

    /// <summary>根据关卡决定进入商店或直接结束关后流程。</summary>
    private void BeginShopOrComplete(int stage)
    {
        if (stage == 4 || stage == 9)
        {
            UIMgr.Instance.ShowPanel<ShopPanel>(E_UILayer.Top, panel =>
            {
                panel.Bind(
                    player != null ? player.RuntimeData : null,
                    progression,
                    player != null ? player.GetComponent<PlayerRelicEffects>() : null,
                    onComplete);
            }, true);
            return;
        }

        onComplete?.Invoke();
    }

    /// <summary>播放一次奖励动画，播放完成后移除奖励对象。</summary>
    private IEnumerator PlayRewardOnce()
    {
        RemoveReward();
        GameObject prefab = Resources.Load<GameObject>(RewardPath);
        if (prefab == null)
            yield break;

        rewardObj = UnityEngine.Object.Instantiate(prefab, GetRewardSpawnPosition(), Quaternion.identity);
        Animator animator = rewardObj != null ? rewardObj.GetComponentInChildren<Animator>() : null;
        if (animator == null)
        {
            yield return new WaitForSecondsRealtime(0.8f);
            RemoveReward();
            yield break;
        }

        animator.enabled = true;
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        animator.speed = 1f;
        animator.Rebind();
        animator.Update(0f);
        animator.Play(0, 0, 0f);
        animator.Update(0f);

        float length = GetRewardAnimationLength(animator);
        float elapsed = 0f;
        while (elapsed < length)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        animator.speed = 0f;
        RemoveReward();
    }

    /// <summary>移除当前奖励动画对象。</summary>
    private void RemoveReward()
    {
        if (rewardObj != null)
            UnityEngine.Object.Destroy(rewardObj);
        rewardObj = null;
    }

    /// <summary>读取奖励动画控制器中最长动画片段的长度。</summary>
    private float GetRewardAnimationLength(Animator animator)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return 0.8f;

        float length = 0f;
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            length = Mathf.Max(length, clip.length);

        return length > 0f ? length : 0.8f;
    }

    /// <summary>计算奖励动画生成位置。</summary>
    private Vector3 GetRewardSpawnPosition()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return Vector3.up * RewardOffsetY;

        Vector3 position = mainCamera.transform.position;
        position.z = 0f;
        position.y += RewardOffsetY;
        return position;
    }
}
