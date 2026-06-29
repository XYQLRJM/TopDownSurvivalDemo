using System.Collections;
using UnityEngine;

/// <summary>
/// 首领火焰区域，玩家停留其中时会持续受伤。
/// </summary>
public class FireHazard : MonoBehaviour
{
    /// <summary>火焰每次造成的伤害。</summary>
    private int damage;
    /// <summary>火焰持续时间，小于等于 0 表示不自动消失。</summary>
    private float lifeTime;
    /// <summary>下一次允许造成伤害的时间。</summary>
    private float nextDamageTime;

    /// <summary>初始化火焰伤害和持续时间。</summary>
    public void Init(int fireDamage, float duration)
    {
        damage = fireDamage;
        lifeTime = duration;
        nextDamageTime = 0f;
        EnsurePhysics();
        StopAllCoroutines();
        if (lifeTime > 0f)
            StartCoroutine(LifeTimer());
    }

    /// <summary>玩家停留在火焰中时持续检测伤害。</summary>
    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamage(other);
    }

    /// <summary>玩家进入火焰时立即尝试结算伤害。</summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamage(other);
    }

    /// <summary>按间隔对触碰火焰的玩家扣血。</summary>
    private void TryDamage(Collider2D other)
    {
        if (Time.time < nextDamageTime)
            return;

        PlayerController2D player = other.GetComponent<PlayerController2D>();
        if (player == null)
            return;

        player.TakeDamage(damage);
        nextDamageTime = Time.time + 0.6f;
    }

    /// <summary>等待持续时间结束后销毁火焰对象。</summary>
    private IEnumerator LifeTimer()
    {
        yield return new WaitForSeconds(lifeTime);
        Destroy(gameObject);
    }

    /// <summary>确保火焰区域带有 2D 触发器和运动学刚体。</summary>
    private void EnsurePhysics()
    {
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
            collider = gameObject.AddComponent<BoxCollider2D>();

        collider.isTrigger = true;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;
    }
}
