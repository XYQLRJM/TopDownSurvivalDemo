using System;
using UnityEngine;

/// <summary>
/// 封装单关倒计时使用的 TimerMgr 调用。
/// </summary>
public class GameStageTimer
{
    /// <summary>当前倒计时在 TimerMgr 中的计时器 id。</summary>
    private int timerKey;
    /// <summary>当前关卡倒计时剩余秒数。</summary>
    private int remainingSeconds;
    /// <summary>倒计时结束时执行的回调。</summary>
    private Action onComplete;
    /// <summary>每次秒数变化时执行的回调。</summary>
    private Action<int> onTick;

    /// <summary>启动一轮关卡倒计时，并绑定刷新和完成回调。</summary>
    public void Start(int durationSeconds, Action<int> tickCallback, Action completeCallback)
    {
        Stop();
        remainingSeconds = Mathf.Max(0, durationSeconds);
        onTick = tickCallback;
        onComplete = completeCallback;
        timerKey = TimerMgr.Instance.CreateTimer(false, remainingSeconds * 1000, Complete, 1000, Tick);
    }

    /// <summary>停止当前倒计时并移除 TimerMgr 中的计时器。</summary>
    public void Stop()
    {
        if (timerKey <= 0)
            return;

        TimerMgr.Instance.RemoveTimer(timerKey);
        timerKey = 0;
    }

    /// <summary>每秒更新剩余时间并通知外部刷新 UI。</summary>
    private void Tick()
    {
        remainingSeconds = Mathf.Max(0, remainingSeconds - 1);
        onTick?.Invoke(remainingSeconds);
    }

    /// <summary>倒计时结束时清理计时器并触发完成回调。</summary>
    private void Complete()
    {
        timerKey = 0;
        remainingSeconds = 0;
        onTick?.Invoke(remainingSeconds);
        onComplete?.Invoke();
    }
}
