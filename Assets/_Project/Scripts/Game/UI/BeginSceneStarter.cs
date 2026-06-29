using UnityEngine;

/// <summary>
/// 使用当前场景内的 UI 对象打开开始菜单。
/// </summary>
public class BeginSceneStarter : MonoBehaviour
{
    /// <summary>全程播放的背景音乐名称。</summary>
    private const string GameMusicName = "game";

    /// <summary>加载开始场景所需的 Canvas、EventSystem 和 BeginPanel。</summary>
    private void Start()
    {
        MusicMgr.Instance.PlayBKMusic(GameMusicName);

        GameObject canvasPrefab = Resources.Load<GameObject>("UI/Canvas");
        GameObject eventSystemPrefab = Resources.Load<GameObject>("UI/EventSystem");
        GameObject beginPanelPrefab = Resources.Load<GameObject>("Prefabs/UI/BeginPanel");

        if (canvasPrefab == null || eventSystemPrefab == null || beginPanelPrefab == null)
        {
            Debug.LogError("Begin scene UI prefab is missing in Resources.");
            return;
        }

        // 这些对象只属于 BeginScene，切换场景时会被销毁。
        GameObject canvas = Instantiate(canvasPrefab);
        Instantiate(eventSystemPrefab);
        Instantiate(beginPanelPrefab, canvas.transform, false);
    }
}
