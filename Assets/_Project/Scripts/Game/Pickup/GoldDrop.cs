using UnityEngine;

/// <summary>
/// 使用对象池管理的金币掉落物，玩家吸附拾取后增加金币并回收到对象池。
/// </summary>
public class GoldDrop : MonoBehaviour
{
    /// <summary>金币预制体在 Resources 中的路径。</summary>
    private const string GoldPath = "Prefabs/Items/gold";
    /// <summary>金币开始被玩家吸附的范围。</summary>
    private const float MagnetRange = 0.9f;
    /// <summary>金币判定拾取成功的距离。</summary>
    private const float PickRange = 0.18f;
    /// <summary>金币吸附移动速度。</summary>
    private const float MagnetSpeed = 7.5f;

    /// <summary>玩家成长组件，用于增加金币数量。</summary>
    private PlayerProgression playerProgression;
    /// <summary>金币吸附的目标玩家位置。</summary>
    private Transform player;
    /// <summary>金币是否已经进入吸附状态。</summary>
    private bool magneting;

    /// <summary>从对象池生成一个金币掉落物。</summary>
    public static void Spawn(Vector3 position)
    {
        GameObject gold = PoolMgr.Instance.GetObj(GoldPath);
        PoolObj poolObj = gold.GetComponent<PoolObj>();
        if (poolObj == null)
            poolObj = gold.AddComponent<PoolObj>();
        if (poolObj.maxNum <= 0)
            poolObj.maxNum = 60;

        gold.transform.position = position;
        SpriteRenderer renderer = gold.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            Color color = renderer.color;
            color.a = 1f;
            renderer.color = color;
            renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, 10);
        }

        GoldDrop drop = gold.GetComponent<GoldDrop>();
        if (drop == null)
            drop = gold.AddComponent<GoldDrop>();
        drop.RefreshTarget();
        drop.magneting = false;
    }

    /// <summary>对象启用时重新寻找玩家目标。</summary>
    private void OnEnable()
    {
        RefreshTarget();
        magneting = false;
    }

    /// <summary>检测吸附范围并移动到玩家身上完成拾取。</summary>
    private void Update()
    {
        if (player == null || playerProgression == null)
            RefreshTarget();

        if (player == null)
            return;

        float distance = Vector2.Distance(transform.position, player.position);
        if (!magneting && distance <= MagnetRange)
            magneting = true;

        if (magneting)
            transform.position = Vector3.MoveTowards(transform.position, player.position, MagnetSpeed * Time.deltaTime);

        if (magneting && distance <= PickRange)
        {
            playerProgression.AddGold(1);
            PoolMgr.Instance.PushObj(gameObject);
        }
    }

    /// <summary>重新查找当前玩家和成长组件。</summary>
    private void RefreshTarget()
    {
        PlayerProgression progression = FindObjectOfType<PlayerProgression>();
        playerProgression = progression;
        player = progression != null ? progression.transform : null;
    }
}
