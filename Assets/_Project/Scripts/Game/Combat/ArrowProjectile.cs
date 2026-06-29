using UnityEngine;

/// <summary>
/// 弓手使用的高速对象池箭矢。
/// </summary>
public class ArrowProjectile : MonoBehaviour
{
    /// <summary>箭矢预制体在 Resources 中的路径。</summary>
    private const string ArrowPath = "Prefabs/Weapon/Arrow";
    /// <summary>箭矢未命中时的最大存活时间。</summary>
    private const float LifeTime = 2f;

    /// <summary>箭矢飞行方向。</summary>
    private Vector2 direction;
    /// <summary>发射者攻击力。</summary>
    private int attack;
    /// <summary>发射者暴击率。</summary>
    private int critRate;
    /// <summary>箭矢飞行速度。</summary>
    private float speed;
    /// <summary>箭袋等遗物带来的单支箭伤害倍率。</summary>
    private float damageMultiplier;
    /// <summary>玩家遗物效果，用来计算暴击倍率、吸血等效果。</summary>
    private PlayerRelicEffects relicEffects;
    /// <summary>箭矢剩余存活时间。</summary>
    private float lifeTimer;
    /// <summary>是否已经命中过目标，避免重复结算。</summary>
    private bool hasHit;

    /// <summary>从对象池取出箭矢并按给定方向发射。</summary>
    public static void Spawn(Vector3 position, Vector2 dir, int playerAttack, int playerCritRate, float projectileSpeed, PlayerRelicEffects effects = null, float projectileDamageMultiplier = 1f)
    {
        GameObject arrow = PoolMgr.Instance.GetObj(ArrowPath);
        if (arrow == null)
            return;

        EnsurePoolObj(arrow);
        EnsurePhysics(arrow);

        arrow.transform.position = position;
        arrow.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

        ArrowProjectile projectile = arrow.GetComponent<ArrowProjectile>();
        if (projectile == null)
            projectile = arrow.AddComponent<ArrowProjectile>();

        projectile.Init(dir, playerAttack, playerCritRate, projectileSpeed, effects, projectileDamageMultiplier);
    }

    /// <summary>初始化箭矢本次发射所需的战斗数据。</summary>
    private void Init(Vector2 dir, int playerAttack, int playerCritRate, float projectileSpeed, PlayerRelicEffects effects, float projectileDamageMultiplier)
    {
        direction = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector2.right;
        attack = playerAttack;
        critRate = playerCritRate;
        speed = projectileSpeed;
        relicEffects = effects;
        damageMultiplier = projectileDamageMultiplier;
        lifeTimer = LifeTime;
        hasHit = false;
    }

    /// <summary>推进箭矢飞行并在超时后回收到对象池。</summary>
    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
            PoolMgr.Instance.PushObj(gameObject);
    }

    /// <summary>箭矢触发碰撞时对怪物或 Boss 造成伤害。</summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit)
            return;

        MonsterController monster = other.GetComponent<MonsterController>();
        if (monster != null && monster.IsAlive)
        {
            hasHit = true;
            float critMultiplier = relicEffects != null ? relicEffects.CritDamageMultiplier : 2f;
            DamageResult result = DamageUtil.CalculateDamageResult(attack, monster.Defense, critRate, critMultiplier, damageMultiplier);
            monster.TakeDamage(result.Damage, transform.position, result.IsCrit);
            relicEffects?.OnDealDamage(result.Damage, result.IsCrit);
            MusicMgr.Instance.PlaySound("Beshoot");
            PoolMgr.Instance.PushObj(gameObject);
            return;
        }

        BossController boss = other.GetComponent<BossController>();
        if (boss == null || !boss.IsAlive)
            return;

        hasHit = true;
        float bossCritMultiplier = relicEffects != null ? relicEffects.CritDamageMultiplier : 2f;
        float bossDamageMultiplier = relicEffects != null ? relicEffects.BossDamageMultiplier : 1f;
        DamageResult bossResult = DamageUtil.CalculateDamageResult(attack, boss.Defense, critRate, bossCritMultiplier, damageMultiplier * bossDamageMultiplier);
        boss.TakeDamage(bossResult.Damage, transform.position, bossResult.IsCrit);
        relicEffects?.OnDealDamage(bossResult.Damage, bossResult.IsCrit);
        MusicMgr.Instance.PlaySound("Beshoot");
        PoolMgr.Instance.PushObj(gameObject);
    }

    /// <summary>确保箭矢带有对象池标记。</summary>
    private static void EnsurePoolObj(GameObject arrow)
    {
        PoolObj poolObj = arrow.GetComponent<PoolObj>();
        if (poolObj == null)
            poolObj = arrow.AddComponent<PoolObj>();
        if (poolObj.maxNum <= 0)
            poolObj.maxNum = 40;
    }

    /// <summary>确保箭矢使用 2D 触发器和运动学刚体。</summary>
    private static void EnsurePhysics(GameObject arrow)
    {
        Collider2D collider = arrow.GetComponent<Collider2D>();
        if (collider != null)
            collider.isTrigger = true;

        Rigidbody2D rb = arrow.GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = arrow.AddComponent<Rigidbody2D>();

        if (rb == null)
            return;

        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;
    }
}
