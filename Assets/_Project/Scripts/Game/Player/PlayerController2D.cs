using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 玩家移动和动画参数驱动。
/// </summary>
public class PlayerController2D : MonoBehaviour
{
    /// <summary>玩家距离地图边缘的最小保留距离。</summary>
    [SerializeField] private float mapEdgePadding = 1.2f;

    /// <summary>出生动画状态名后缀。</summary>
    private const string BornStateSuffix = "_Born";
    /// <summary>攻击动画状态名后缀。</summary>
    private const string AttackStateSuffix = "_Attack";
    /// <summary>受击动画状态名后缀。</summary>
    private const string SufferStateSuffix = "_Suffer";
    /// <summary>死亡动画状态名后缀。</summary>
    private const string DeadStateSuffix = "_Dead";
    /// <summary>动画机移动速度参数。</summary>
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    /// <summary>动画机攻击触发参数。</summary>
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    /// <summary>动画机受击触发参数。</summary>
    private static readonly int SufferHash = Animator.StringToHash("Suffer");
    /// <summary>动画机死亡触发参数。</summary>
    private static readonly int DieHash = Animator.StringToHash("Die");

    /// <summary>玩家动画机组件。</summary>
    private Animator animator;
    /// <summary>玩家精灵渲染器，用于左右翻转。</summary>
    private SpriteRenderer spriteRenderer;
    /// <summary>玩家运行时属性。</summary>
    private PlayerRuntimeData runtimeData;
    /// <summary>玩家已获得的遗物效果。</summary>
    private PlayerRelicEffects relicEffects;
    /// <summary>玩家允许移动的地图边界。</summary>
    private Bounds moveBounds;
    /// <summary>玩家死亡后的回调。</summary>
    private Action<PlayerController2D> onDead;
    /// <summary>当前角色动画片段名前缀。</summary>
    private string animPrefix;
    /// <summary>玩家最后一次有效朝向。</summary>
    private Vector2 lastLookDir = Vector2.right;
    /// <summary>是否正在播放出生动画。</summary>
    private bool isBornPlaying = true;
    /// <summary>玩家是否已经死亡。</summary>
    private bool isDead;

    /// <summary>摄像机追随玩家时使用的目标点。</summary>
    public Transform CameraTarget => transform;
    /// <summary>玩家当前攻击朝向。</summary>
    public Vector2 LookDirection => lastLookDir;
    /// <summary>玩家运行时属性。</summary>
    public PlayerRuntimeData RuntimeData => runtimeData;

    /// <summary>初始化玩家属性、地图限制、动画状态和死亡回调。</summary>
    public void Init(CharacterConfig config, Bounds mapBounds, Action<PlayerController2D> deadCallback)
    {
        runtimeData = gameObject.AddComponent<PlayerRuntimeData>();
        runtimeData.Init(config);
        relicEffects = gameObject.GetComponent<PlayerRelicEffects>();

        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        moveBounds = mapBounds;
        onDead = deadCallback;
        animPrefix = string.IsNullOrEmpty(config.animPrefix) ? "P001" : config.animPrefix;

        ResetAnimatorParameters();
        StartCoroutine(WaitBorn());
    }

    /// <summary>出生结束后按输入移动玩家。</summary>
    private void Update()
    {
        if (isDead)
            return;

        if (!isBornPlaying)
            MoveByInput();
    }

    /// <summary>触发玩家攻击动画。</summary>
    public void TriggerAttack()
    {
        if (!isBornPlaying && !isDead)
            StartCoroutine(TriggerAction(AttackHash, AttackStateSuffix));
    }

    /// <summary>玩家受到伤害，生命归零时进入死亡流程。</summary>
    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        runtimeData.TakeDamage(damage);
        MusicMgr.Instance.PlaySound("hit_impact");
        StartCoroutine(TriggerAction(SufferHash, SufferStateSuffix));
        if (runtimeData.CurrentHp <= 0 && (relicEffects == null || !relicEffects.TryRevive()))
            StartCoroutine(TriggerDead());
    }

    /// <summary>读取 WASD/方向键输入并移动玩家。</summary>
    private void MoveByInput()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        Vector2 moveDir = input.sqrMagnitude > 1f ? input.normalized : input;

        if (moveDir.sqrMagnitude > 0.01f)
        {
            transform.position += (Vector3)(moveDir * runtimeData.MoveSpeed * Time.deltaTime);
            ClampInsideMap();

            if (Mathf.Abs(moveDir.x) > 0.01f)
            {
                lastLookDir = moveDir.x > 0 ? Vector2.right : Vector2.left;
                if (spriteRenderer != null)
                    spriteRenderer.flipX = moveDir.x < 0;
            }
        }

        if (animator != null)
            animator.SetFloat(SpeedHash, moveDir.magnitude);
    }

    /// <summary>等待出生动画播放完成。</summary>
    private IEnumerator WaitBorn()
    {
        isBornPlaying = true;
        yield return new WaitForSeconds(GetClipLength(BornStateSuffix));
        isBornPlaying = false;
    }

    /// <summary>触发一次攻击或受击动作动画。</summary>
    private IEnumerator TriggerAction(int triggerHash, string stateSuffix)
    {
        if (isBornPlaying || isDead)
            yield break;

        animator?.SetTrigger(triggerHash);
        yield return new WaitForSeconds(GetClipLength(stateSuffix));
    }

    /// <summary>播放死亡动画并通知关卡管理器。</summary>
    private IEnumerator TriggerDead()
    {
        isDead = true;
        if (animator != null)
        {
            animator.SetFloat(SpeedHash, 0f);
            animator.SetTrigger(DieHash);
        }
        yield return new WaitForSeconds(GetClipLength(DeadStateSuffix));
        onDead?.Invoke(this);
        Destroy(gameObject);
    }

    /// <summary>重置动画机参数，避免对象创建后残留触发器状态。</summary>
    private void ResetAnimatorParameters()
    {
        if (animator == null)
            return;

        animator.SetFloat(SpeedHash, 0f);
        animator.ResetTrigger(AttackHash);
        animator.ResetTrigger(SufferHash);
        animator.ResetTrigger(DieHash);
    }

    /// <summary>按状态名后缀查找动画片段长度。</summary>
    private float GetClipLength(string suffix)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return 0.3f;

        string clipName = animPrefix + suffix;
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName)
                return clip.length;
        }

        return 0.3f;
    }

    /// <summary>限制玩家位置，避免完全贴住地图边界。</summary>
    private void ClampInsideMap()
    {
        if (moveBounds.size == Vector3.zero)
            return;

        Vector3 pos = transform.position;
        // 与地图边缘保持少量距离，保证靠近镜头边界时角色整体仍可见。
        pos.x = Mathf.Clamp(pos.x, moveBounds.min.x + mapEdgePadding, moveBounds.max.x - mapEdgePadding);
        pos.y = Mathf.Clamp(pos.y, moveBounds.min.y + mapEdgePadding, moveBounds.max.y - mapEdgePadding);
        transform.position = pos;
    }

    /// <summary>根据角色预制体路径选择动画名前缀。</summary>
}
