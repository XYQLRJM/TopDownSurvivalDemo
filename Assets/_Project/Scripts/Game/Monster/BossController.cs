using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 通用 Boss 控制器。Boss1 召唤小怪，Boss2 冲撞并生成火焰区域。
/// </summary>
public class BossController : MonoBehaviour
{
    /// <summary>Boss1 召唤小怪时使用的预警预制体路径。</summary>
    private const string TipPath = "Prefabs/Character/Monstertips";
    /// <summary>Boss2 火焰技能预警预制体路径。</summary>
    private const string BeforeFirePath = "Prefabs/Weapon/BeforeFire";
    /// <summary>Boss2 火焰伤害区域预制体路径。</summary>
    private const string FirePath = "Prefabs/Weapon/Fire";
    /// <summary>Boss1 搜索玩家的范围。</summary>
    private const float Boss1DetectRange = 26f;
    /// <summary>Boss2 搜索玩家的范围。</summary>
    private const float Boss2DetectRange = 32f;
    /// <summary>Boss 接触伤害间隔。</summary>
    private const float ContactDamageInterval = 0.8f;
    /// <summary>Boss 主动攻击间隔。</summary>
    private const float AttackInterval = 1.8f;
    /// <summary>Boss1 扇形攻击范围。</summary>
    private const float Boss1AttackRange = 3.2f;
    /// <summary>Boss2 冲撞触发范围。</summary>
    private const float Boss2AttackRange = 8f;
    /// <summary>Boss1 扇形攻击左右各自角度。</summary>
    private const float Boss1AttackHalfAngle = 30f;
    /// <summary>Boss1 小怪预警持续时间。</summary>
    private const float TipDuration = 2f;
    /// <summary>Boss2 火焰预警持续时间。</summary>
    private const float BeforeFireDuration = 2f;
    /// <summary>Boss1 移速公式中使用的基础移速。</summary>
    private const float WarriorBaseMoveSpeed = 6f;
    /// <summary>Boss1 半血召唤小怪的环形半径。</summary>
    private const float MinionSpawnRadius = 4.5f;
    /// <summary>Boss 出生点附近游走半径。</summary>
    private const float WanderRadius = 2.2f;
    /// <summary>Boss 游走速度比例。</summary>
    private const float WanderSpeedRate = 0.35f;
    /// <summary>Boss 随机游走方向切换间隔。</summary>
    private const float WanderChangeInterval = 1.4f;
    /// <summary>Boss 动画机移动速度参数。</summary>
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    /// <summary>当前 Boss 配置。</summary>
    private MonsterConfig config;
    /// <summary>Boss 追踪和攻击的玩家。</summary>
    private PlayerController2D player;
    /// <summary>刷怪器，用于 Boss1 半血召唤小怪。</summary>
    private MonsterSpawner monsterSpawner;
    /// <summary>地图边界，用于限制 Boss 和技能生成位置。</summary>
    private Bounds mapBounds;
    /// <summary>Boss 精灵渲染器，用于朝向翻转。</summary>
    private SpriteRenderer spriteRenderer;
    /// <summary>Boss 碰撞体，用于接触伤害。</summary>
    private Collider2D hitCollider;
    /// <summary>Boss 动画机。</summary>
    private Animator animator;
    /// <summary>Boss 死亡后的回调。</summary>
    private Action<BossController> onDead;
    /// <summary>Boss 当前生命。</summary>
    private int hp;
    /// <summary>Boss 实际移动速度。</summary>
    private float moveSpeed;
    /// <summary>下一次允许造成接触伤害的时间。</summary>
    private float nextContactDamageTime;
    /// <summary>下一次允许主动攻击的时间。</summary>
    private float nextAttackTime;
    /// <summary>下一次切换游走方向的时间。</summary>
    private float nextWanderTime;
    /// <summary>Boss 当前朝向。</summary>
    private Vector2 facing = Vector2.right;
    /// <summary>Boss 当前游走方向。</summary>
    private Vector2 wanderDir = Vector2.right;
    /// <summary>Boss 出生点，用于限制小范围游走。</summary>
    private Vector3 spawnPoint;
    /// <summary>半血技能是否已经触发过。</summary>
    private bool halfHpSkillUsed;
    /// <summary>Boss 是否处于死亡流程。</summary>
    private bool dying;
    /// <summary>Boss 是否正在释放半血技能。</summary>
    private bool casting;
    /// <summary>Boss2 是否正在冲撞。</summary>
    private bool charging;

    /// <summary>Boss 当前生命。</summary>
    public int CurrentHp => hp;
    /// <summary>Boss 最大生命。</summary>
    public int MaxHp => config != null ? config.maxHp : 1;
    /// <summary>Boss 防御力。</summary>
    public int Defense => config != null ? config.defense : 0;
    /// <summary>Boss 是否存活。</summary>
    public bool IsAlive => !dying && hp > 0;
    /// <summary>当前 Boss 是否是 Boss2。</summary>
    private bool IsBoss2 => config != null && config.id == "boss_2";

    /// <summary>初始化 Boss 属性、目标、地图边界和死亡回调。</summary>
    public void Init(MonsterConfig bossConfig, PlayerController2D target, MonsterSpawner spawner, Bounds bounds, Action<BossController> deadCallback)
    {
        config = bossConfig;
        player = target;
        monsterSpawner = spawner;
        mapBounds = bounds;
        onDead = deadCallback;
        hp = config.maxHp;
        moveSpeed = IsBoss2 ? 20f : WarriorBaseMoveSpeed * (config.moveSpeed + 5f) / 10f;
        spriteRenderer = GetComponent<SpriteRenderer>();
        hitCollider = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
        spawnPoint = transform.position;
        nextContactDamageTime = 0f;
        nextAttackTime = Time.time + 1f;
        nextWanderTime = 0f;
        halfHpSkillUsed = false;
        dying = false;
        casting = false;
        charging = false;

        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 1f;
            spriteRenderer.color = color;
        }

        if (hitCollider != null)
            hitCollider.enabled = true;

        animator?.SetFloat(SpeedHash, 0f);
        if (animator != null)
            PlayState("Idle");
    }

    /// <summary>每帧执行索敌、攻击、游走和半血技能检测。</summary>
    private void Update()
    {
        if (!IsAlive || player == null)
            return;

        // 半血技能准备期间 Boss 免疫伤害。
        if (!halfHpSkillUsed && hp <= MaxHp * 0.5f)
            StartCoroutine(IsBoss2 ? CastBoss2FireSkill() : CastBoss1SummonSkill());

        Vector2 toPlayer = player.transform.position - transform.position;
        if (toPlayer.sqrMagnitude > 0.01f)
            FaceDirection(toPlayer.normalized);

        if (casting || charging)
        {
            animator?.SetFloat(SpeedHash, 0f);
            return;
        }

        float detectRange = IsBoss2 ? Boss2DetectRange : Boss1DetectRange;
        if (toPlayer.magnitude <= detectRange)
        {
            if (IsBoss2)
                TryBoss2Charge(toPlayer.normalized, toPlayer.magnitude);
            else
                UpdateBoss1Combat(toPlayer.normalized, toPlayer.magnitude);
        }
        else
        {
            WanderNearSpawn();
        }

        TryContactDamage();
    }

    /// <summary>Boss 受到伤害，释放技能时会免疫伤害。</summary>
    public void TakeDamage(int damage, Vector2 fromPosition, bool isCrit = false)
    {
        if (!IsAlive)
            return;

        if (casting)
        {
            DamageText.Show(transform.position + Vector3.up * 1.2f, 0, false);
            return;
        }

        hp -= damage;
        DamageText.Show(transform.position + Vector3.up * 1.2f, damage, isCrit);
        PlayState("Suffer");
        if (hp <= 0)
            StartCoroutine(Die());
    }

    /// <summary>更新 Boss1 追击和扇形攻击行为。</summary>
    private void UpdateBoss1Combat(Vector2 dir, float distance)
    {
        if (distance > Boss1AttackRange * 0.65f)
            Move(dir, moveSpeed);
        else
            animator?.SetFloat(SpeedHash, 0f);

        TryBoss1ConeAttack(dir, distance);
    }

    /// <summary>Boss1 尝试在扇形范围内攻击玩家。</summary>
    private void TryBoss1ConeAttack(Vector2 toPlayer, float distance)
    {
        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + AttackInterval;
        PlayState("Attack");

        if (Vector2.Angle(facing, toPlayer) > Boss1AttackHalfAngle || distance > Boss1AttackRange)
            return;

        int damage = Mathf.Max(1, config.attack - player.RuntimeData.Defense);
        player.TakeDamage(damage);
    }

    /// <summary>Boss2 在玩家进入范围后尝试冲撞。</summary>
    private void TryBoss2Charge(Vector2 dir, float distance)
    {
        if (distance > Boss2AttackRange || Time.time < nextAttackTime)
        {
            WanderNearSpawn();
            return;
        }

        nextAttackTime = Time.time + AttackInterval;
        StartCoroutine(Charge(dir));
    }

    /// <summary>Boss2 向玩家方向短距离高速冲撞。</summary>
    private IEnumerator Charge(Vector2 dir)
    {
        charging = true;
        PlayState("Attack");
        float elapsed = 0f;
        while (elapsed < 0.45f && IsAlive)
        {
            elapsed += Time.deltaTime;
            Move(dir, moveSpeed);
            TryContactDamage();
            yield return null;
        }

        charging = false;
        animator?.SetFloat(SpeedHash, 0f);
    }

    /// <summary>检测 Boss 与玩家碰撞并造成接触伤害。</summary>
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

    /// <summary>Boss1 半血时预警并召唤九只小怪。</summary>
    private IEnumerator CastBoss1SummonSkill()
    {
        halfHpSkillUsed = true;
        casting = true;
        animator?.SetFloat(SpeedHash, 0f);

        // 先显示所有预警，再生成小怪，让玩家有反应时间。
        string[] ids =
        {
            "monster_1", "monster_1", "monster_1",
            "monster_2", "monster_2", "monster_2",
            "monster_3", "monster_3", "monster_3"
        };

        GameObject[] tips = new GameObject[ids.Length];
        Vector3[] positions = new Vector3[ids.Length];
        for (int i = 0; i < ids.Length; ++i)
        {
            float angle = i * Mathf.PI * 2f / ids.Length;
            positions[i] = ClampInsideMap(transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * MinionSpawnRadius);
            tips[i] = PoolMgr.Instance.GetObj(TipPath);
            EnsurePoolObj(tips[i], ids.Length);
            tips[i].transform.position = positions[i];
        }

        yield return new WaitForSeconds(TipDuration);

        for (int i = 0; i < tips.Length; ++i)
        {
            if (tips[i] != null)
                PoolMgr.Instance.PushObj(tips[i]);
        }

        if (IsAlive)
        {
            for (int i = 0; i < ids.Length; ++i)
                monsterSpawner?.SpawnMonsterImmediate(ids[i], positions[i]);
        }

        casting = false;
    }

    /// <summary>Boss2 半血时随机预警并生成火焰区域。</summary>
    private IEnumerator CastBoss2FireSkill()
    {
        halfHpSkillUsed = true;
        casting = true;
        animator?.SetFloat(SpeedHash, 0f);

        // 火焰预警使用普通场景对象，因为当前预制体不需要对象池管理。
        Vector3[] positions = GetRandomFirePositions(9);
        GameObject warningPrefab = Resources.Load<GameObject>(BeforeFirePath);
        GameObject[] warnings = new GameObject[positions.Length];
        for (int i = 0; i < positions.Length; ++i)
        {
            if (warningPrefab == null)
                continue;

            warnings[i] = Instantiate(warningPrefab, positions[i], Quaternion.identity);
        }

        yield return new WaitForSeconds(BeforeFireDuration);

        GameObject firePrefab = Resources.Load<GameObject>(FirePath);
        for (int i = 0; i < warnings.Length; ++i)
        {
            if (warnings[i] != null)
                Destroy(warnings[i]);

            if (firePrefab == null)
                continue;

            GameObject fire = Instantiate(firePrefab, positions[i], Quaternion.identity);
            FireHazard hazard = fire.GetComponent<FireHazard>();
            if (hazard == null)
                hazard = fire.AddComponent<FireHazard>();
            hazard.Init(5, -1f);
        }

        casting = false;
    }

    /// <summary>随机获取不重叠的火焰生成位置。</summary>
    private Vector3[] GetRandomFirePositions(int count)
    {
        Vector3[] positions = new Vector3[count];
        float minDistance = 2.2f;
        for (int i = 0; i < count; ++i)
        {
            Vector3 pos = mapBounds.center;
            for (int tries = 0; tries < 40; ++tries)
            {
                pos = new Vector3(
                    UnityEngine.Random.Range(mapBounds.min.x + 1f, mapBounds.max.x - 1f),
                    UnityEngine.Random.Range(mapBounds.min.y + 1f, mapBounds.max.y - 1f),
                    0f);

                bool tooClose = false;
                for (int j = 0; j < i; ++j)
                {
                    if (Vector2.Distance(pos, positions[j]) < minDistance)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                    break;
            }

            positions[i] = pos;
        }

        return positions;
    }

    /// <summary>让 Boss 在出生点附近小范围游走。</summary>
    private void WanderNearSpawn()
    {
        if (Time.time >= nextWanderTime)
        {
            wanderDir = UnityEngine.Random.insideUnitCircle.normalized;
            if (wanderDir.sqrMagnitude < 0.01f)
                wanderDir = Vector2.right;
            nextWanderTime = Time.time + WanderChangeInterval;
        }

        Vector3 next = transform.position + (Vector3)(wanderDir * moveSpeed * WanderSpeedRate * Time.deltaTime);
        if (Vector2.Distance(next, spawnPoint) > WanderRadius)
            wanderDir = ((Vector2)spawnPoint - (Vector2)transform.position).normalized;

        Move(wanderDir, moveSpeed * WanderSpeedRate);
    }

    /// <summary>按方向和速度移动 Boss，并限制在地图内。</summary>
    private void Move(Vector2 dir, float speed)
    {
        transform.position += (Vector3)(dir.normalized * speed * Time.deltaTime);
        transform.position = ClampInsideMap(transform.position);
        animator?.SetFloat(SpeedHash, speed > 0.01f ? 1f : 0f);
    }

    /// <summary>更新 Boss 朝向并翻转精灵。</summary>
    private void FaceDirection(Vector2 dir)
    {
        facing = dir.sqrMagnitude > 0.01f ? dir.normalized : facing;
        if (spriteRenderer != null && Mathf.Abs(facing.x) > 0.01f)
            spriteRenderer.flipX = facing.x < 0;
    }

    /// <summary>把位置限制在地图范围内。</summary>
    private Vector3 ClampInsideMap(Vector3 pos)
    {
        if (mapBounds.size == Vector3.zero)
            return pos;

        pos.x = Mathf.Clamp(pos.x, mapBounds.min.x + 0.8f, mapBounds.max.x - 0.8f);
        pos.y = Mathf.Clamp(pos.y, mapBounds.min.y + 0.8f, mapBounds.max.y - 0.8f);
        pos.z = 0f;
        return pos;
    }

    /// <summary>确保池化预警对象有 PoolObj 标记。</summary>
    private void EnsurePoolObj(GameObject obj, int maxNum)
    {
        if (obj == null)
            return;

        PoolObj poolObj = obj.GetComponent<PoolObj>();
        if (poolObj == null)
            poolObj = obj.AddComponent<PoolObj>();
        if (poolObj.maxNum <= 0)
            poolObj.maxNum = maxNum;
    }

    /// <summary>播放死亡动画、通知关卡管理器并销毁 Boss。</summary>
    private IEnumerator Die()
    {
        dying = true;
        if (hitCollider != null)
            hitCollider.enabled = false;

        animator?.SetFloat(SpeedHash, 0f);
        PlayState("Dead");
        yield return new WaitForSeconds(0.35f);
        onDead?.Invoke(this);
        Destroy(gameObject);
    }

    /// <summary>按 Boss 类型播放对应动画状态。</summary>
    private void PlayState(string state)
    {
        if (animator == null || config == null)
            return;

        // 动画状态名按 Boss 预制体分组，例如 Boss1_Idle、Boss2_Idle。
        string prefix = IsBoss2 ? "Boss2" : "Boss1";
        animator.Play($"{prefix}_{state}", 0, 0f);
    }
}
