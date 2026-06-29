using System.Collections;
using UnityEngine;

/// <summary>
/// 世界空间中使用对象池管理的伤害文本。
/// </summary>
public class DamageText : MonoBehaviour
{
    /// <summary>伤害文本预制体在 Resources 中的路径。</summary>
    private const string DamageTextPath = "Prefabs/UI/DamageText";
    /// <summary>伤害文本存在时间。</summary>
    private const float LifeTime = 0.65f;
    /// <summary>伤害文本字号。</summary>
    private const int FontSize = 64;
    /// <summary>TextMesh 字符尺寸。</summary>
    private const float CharacterSize = 0.11f;

    /// <summary>显示伤害数字的 TextMesh。</summary>
    private TextMesh textMesh;

    /// <summary>从对象池显示一次伤害数字。</summary>
    public static void Show(Vector3 position, int damage, bool isCrit = false)
    {
        GameObject obj = PoolMgr.Instance.GetObj(DamageTextPath);
        PoolObj poolObj = obj.GetComponent<PoolObj>();
        if (poolObj == null)
            poolObj = obj.AddComponent<PoolObj>();
        if (poolObj.maxNum <= 0)
            poolObj.maxNum = 50;

        obj.transform.position = position;
        DamageText text = obj.GetComponent<DamageText>();
        if (text == null)
            text = obj.AddComponent<DamageText>();
        text.Play(damage, isCrit);
    }

    /// <summary>初始化 TextMesh 组件。</summary>
    private void Awake()
    {
        textMesh = GetComponent<TextMesh>();
        if (textMesh == null)
        {
            textMesh = gameObject.AddComponent<TextMesh>();
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = FontSize;
            textMesh.characterSize = CharacterSize;
        }
    }

    /// <summary>设置伤害文本内容、颜色并开始飘字动画。</summary>
    private void Play(int damage, bool isCrit)
    {
        if (textMesh == null)
            textMesh = GetComponent<TextMesh>();
        if (textMesh == null)
            Awake();

        if (textMesh != null)
        {
            textMesh.text = $"-{damage}";
            textMesh.color = isCrit ? Color.yellow : Color.red;
            textMesh.fontSize = FontSize;
            textMesh.characterSize = CharacterSize;
        }

        StopAllCoroutines();
        StartCoroutine(Floating());
    }

    /// <summary>让伤害文本向上漂浮并逐渐淡出。</summary>
    private IEnumerator Floating()
    {
        float time = 0f;
        Vector3 start = transform.position;
        while (time < LifeTime)
        {
            time += Time.deltaTime;
            transform.position = start + Vector3.up * time;
            if (textMesh != null)
            {
                Color color = textMesh.color;
                color.a = Mathf.Lerp(1f, 0f, time / LifeTime);
                textMesh.color = color;
            }
            yield return null;
        }

        PoolMgr.Instance.PushObj(gameObject);
    }
}
