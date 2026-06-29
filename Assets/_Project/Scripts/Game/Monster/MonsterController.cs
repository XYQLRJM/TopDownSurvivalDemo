using System.Collections;
using UnityEngine;

/// <summary>
/// 怪物运行时行为：检测玩家、追击、受伤、淡出并回收到对象池。
/// </summary>
public class MonsterController : MonoBehaviour
{
    /// <summary>玩家移速公式中使用的基础移速。</summary>
    private const float WarriorBaseMoveSpeed = 6f;
    /// <summary>怪物发现玩家的半径。</summary>
    private const float DetectRange = 11f;
    /// <summary>怪物碰撞伤害的间隔。</summary>
    private const float ContactDamageInterval = 0.8f;
    /// <summary>怪物受击但未死亡时的击退距离。</summary>
    private const float KnockbackDistance = 0.35f;
    /// <summary>怪物死亡淡出时间。</summary>
    private const float FadeDuration = 1f;
    /// <summary>怪物随机游走相对正常移速的比例。</summary>
    private const float WanderSpeedRate = 0.35f;
    /// <summary>怪物随机游走方向的最短保持时间。</summary>
    private const float MinWanderChangeTime = 1.2f;
    /// <summary>怪物随机游走方向的最长保持时间。</summary>
    private const float MaxWanderChangeTime = 2.5f;
    /// <summary>远程怪物发射火球的间隔。</summary>
    private const float FireballInterval = 2f;
    /// <summary>远程怪物火球速度。</summary>
    private const float FireballSpeed = 4.5f;
    /// <summary>火球生成点相对怪物中心的偏移。</summary>
    private const float FireballMuzzleOffset = 0.45f;

    /// <summary>当前怪物配置。</summary>
    private MonsterConfig config;
    /// <summary>怪物追踪和攻击的玩家。</summary>
    private PlayerController2D player;
    /// <summary>玩家成长组件，用于怪物死亡时增加经验。</summary>
    private PlayerProgression playerProgression;
    /// <summary>怪物精灵渲染器，用于翻转和淡出。</summary>
    private SpriteRenderer spriteRenderer;
    /// <summary>怪物碰撞体，用于接触伤害。</summary>
    private Collider2D hitCollider;
    /// <summary>怪物当前生命。</summary>
    private int hp;
    /// <summary>怪物实际移动速度。</summary>
    private float moveSpeed;
    /// <summary>下一次允许造成碰撞伤害的时间。</summary>
    private float nextContactDamageTime;
    /// <summary>下一次允许发射火球的时间。</summary>
    private float nextFireballTime;
    /// <summary>下一次更换游走方向的时间。</summary>
    private float nextWanderChangeTime;
    /// <summary>当前随机游走方向。</summary>
    private Vector2 wanderDir;
    /// <summary>怪物是否正在死亡流程中。</summary>
    private bool dying;

    /// <summary>怪物防御力。</summary>
    public int Defense => config != null ? config.defense : 0;
    /// <summary>怪物是否仍然存活。</summary>
    public bool IsAlive => !dying && hp > 0;

    /// <summary>初始化怪物配置、目标玩家和运行时状态。</summary>
    public void Init(MonsterConfig monsterConfig, PlayerController2D target, PlayerProgression progression)
    {
        config = monsterConfig;
        player = target;
        playerProgression = progression;
        spriteRenderer = GetComponent<SpriteRenderer>();
        hitCollider = GetComponent<Collider2D>();
        hp = config.maxHp;
        moveSpeed = WarriorBaseMoveSpeed * (config.moveSpeed + 5f) / 10f;
        nextContactDamageTime = 0f;
        nextFireballTime = Time.time + Random.Range(0.4f, 1.2f);
        PickWanderDirection();
        dying = false;

        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 1f;
            spriteRenderer.color = color;
        }

        if (hitCollider != null)
            hitCollider.enabled = true;

        EnsurePoolObj();
    }

    /// <summary>每帧执行索敌、追击、远程攻击和接触伤害。</summary>
    private void Update()
    {
        if (!IsAlive || player == null)
            return;

        Vector2 toPlayer = player.transform.position - transform.position;
        if (toPlayer.magnitude <= DetectRange)
        {
            Vector2 dir = toPlayer.normalized;
            if (IsRangedMonster)
                TryFireballAttack(dir);
            else
                Move(dir, moveSpeed);
        }
        else
        {
            Wander();
        }

        TryContactDamage();
    }

    /// <summary>让怪物受到伤害，死亡时掉落金币并给经验。</summary>
    public void TakeDamage(int damage, Vector2 fromPosition, bool isCrit = false)
    {
        if (!IsAlive)
            return;

        hp -= damage;
        DamageText.Show(transform.position + Vector3.up * 0.8f, damage, isCrit);

        if (hp <= 0)
        {
            StartCoroutine(Die());
            return;
        }

        Vector2 dir = ((Vector2)transform.position - fromPosition).normalized;
        transform.position += (Vector3)(dir * KnockbackDistance);
    }

    /// <summary>检测玩家碰撞并按间隔造成接触伤害。</summary>
    private void TryContactDamage()
    {
        if (Time.time < nextContactDamageTime || player == null)
            return;

        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if (hitCollider == null || playerCollider == null)
            return;

        if (hitCollider.bounds.Intersects(playerCollider.bounds))
        {
            int damage = Mathf.Max(1, config.attack - player.RuntimeData.Defense);
            player.TakeDamage(damage);
            nextContactDamageTime = Time.time + ContactDamageInterval;
        }
    }

    /// <summary>当前怪物是否使用远程火球行为。</summary>
    private bool IsRangedMonster => config != null && config.behaviorType == "ranged_fireball";

    /// <summary>远程怪物尝试向玩家方向发射火球。</summary>
    private void TryFireballAttack(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.01f)
            return;

        FaceDirection(dir);
        Wander();

        if (Time.time < nextFireballTime)
            return;

        FireballProjectile.Spawn(transform.position + (Vector3)(dir.normalized * FireballMuzzleOffset), dir, config.attack, FireballSpeed);
        nextFireballTime = Time.time + FireballInterval;
    }

    /// <summary>以较低速度随机游走。</summary>
    private void Wander()
    {
        if (Time.time >= nextWanderChangeTime || wanderDir.sqrMagnitude < 0.01f)
            PickWanderDirection();

        Move(wanderDir, moveSpeed * WanderSpeedRate);
    }

    /// <summary>随机选择一个新的游走方向。</summary>
    private void PickWanderDirection()
    {
        wanderDir = Random.insideUnitCircle.normalized;
        if (wanderDir.sqrMagnitude < 0.01f)
            wanderDir = Vector2.right;

        nextWanderChangeTime = Time.time + Random.Range(MinWanderChangeTime, MaxWanderChangeTime);
    }

    /// <summary>按照方向和速度移动怪物。</summary>
    private void Move(Vector2 dir, float speed)
    {
        if (dir.sqrMagnitude < 0.01f)
            return;

        transform.position += (Vector3)(dir.normalized * speed * Time.deltaTime);
        FaceDirection(dir);
    }

    /// <summary>根据移动方向翻转怪物精灵。</summary>
    private void FaceDirection(Vector2 dir)
    {
        if (spriteRenderer != null && Mathf.Abs(dir.x) > 0.01f)
            spriteRenderer.flipX = dir.x < 0;
    }

    /// <summary>怪物死亡后发放奖励、淡出并回收到对象池。</summary>
    private IEnumerator Die()
    {
        dying = true;
        if (hitCollider != null)
            hitCollider.enabled = false;

        playerProgression?.AddExperience(GetExperienceReward());
        GoldDrop.Spawn(transform.position);

        float time = 0f;
        Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        while (time < FadeDuration)
        {
            time += Time.deltaTime;
            if (spriteRenderer != null)
            {
                Color color = startColor;
                color.a = Mathf.Lerp(1f, 0f, time / FadeDuration);
                spriteRenderer.color = color;
            }
            yield return null;
        }

        PoolMgr.Instance.PushObj(gameObject);
    }

    /// <summary>确保怪物带有对象池标记。</summary>
    private void EnsurePoolObj()
    {
        PoolObj poolObj = GetComponent<PoolObj>();
        if (poolObj == null)
            poolObj = gameObject.AddComponent<PoolObj>();
        if (poolObj.maxNum <= 0)
            poolObj.maxNum = 30;
    }

    /// <summary>根据怪物类型计算击杀经验奖励。</summary>
    private int GetExperienceReward()
    {
        int reward = 3;
        if (config == null)
            return reward;

        switch (config.id)
        {
            case "monster_3":
            case "monster_4":
                return reward + 1;
            case "monster_5":
            case "monster_6":
                return reward + 2;
            default:
                return reward;
        }
    }
}
