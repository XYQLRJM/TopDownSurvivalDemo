using UnityEngine;

/// <summary>
/// 使用对象池管理的怪物火球弹体。
/// </summary>
public class FireballProjectile : MonoBehaviour
{
    /// <summary>火球预制体在 Resources 中的路径。</summary>
    private const string FireballPath = "Prefabs/Weapon/FireBall";
    /// <summary>火球未命中时的最大存活时间。</summary>
    private const float LifeTime = 4f;

    /// <summary>火球飞行方向。</summary>
    private Vector2 direction;
    /// <summary>怪物攻击力。</summary>
    private int attack;
    /// <summary>火球飞行速度。</summary>
    private float speed;
    /// <summary>火球剩余存活时间。</summary>
    private float lifeTimer;
    /// <summary>是否已经命中过玩家。</summary>
    private bool hasHit;

    /// <summary>从对象池取出火球并向指定方向发射。</summary>
    public static void Spawn(Vector3 position, Vector2 dir, int monsterAttack, float projectileSpeed)
    {
        GameObject fireball = PoolMgr.Instance.GetObj(FireballPath);
        if (fireball == null)
            return;

        EnsurePhysics(fireball);
        fireball.transform.position = position;
        fireball.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

        FireballProjectile projectile = fireball.GetComponent<FireballProjectile>();
        if (projectile == null)
            projectile = fireball.AddComponent<FireballProjectile>();

        projectile.Init(dir, monsterAttack, projectileSpeed);
    }

    /// <summary>初始化本次火球攻击数据。</summary>
    private void Init(Vector2 dir, int monsterAttack, float projectileSpeed)
    {
        direction = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector2.right;
        attack = monsterAttack;
        speed = projectileSpeed;
        lifeTimer = LifeTime;
        hasHit = false;
    }

    /// <summary>推进火球飞行并在超时后回收到对象池。</summary>
    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
            PoolMgr.Instance.PushObj(gameObject);
    }

    /// <summary>火球命中玩家时造成一次伤害。</summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit)
            return;

        PlayerController2D player = other.GetComponent<PlayerController2D>();
        if (player == null || player.RuntimeData == null)
            return;

        hasHit = true;
        int damage = Mathf.Max(1, attack - player.RuntimeData.Defense);
        player.TakeDamage(damage);
        PoolMgr.Instance.PushObj(gameObject);
    }

    /// <summary>确保火球使用 2D 触发器和运动学刚体。</summary>
    private static void EnsurePhysics(GameObject fireball)
    {
        Collider2D collider = fireball.GetComponent<Collider2D>();
        if (collider != null)
            collider.isTrigger = true;

        Rigidbody2D rb = fireball.GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = fireball.AddComponent<Rigidbody2D>();

        if (rb == null)
            return;

        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;
    }
}
