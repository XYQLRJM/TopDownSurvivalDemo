using UnityEngine;

/// <summary>
/// 创建 GameScene 所需运行时对象，并把关卡流程交给 GameLevelManager。
/// </summary>
public class GameSceneStarter : MonoBehaviour
{
    /// <summary>游戏场景播放的背景音乐名称。</summary>
    private const string GameMusicName = "game";

    /// <summary>玩家在选角界面选择的角色配置。</summary>
    private CharacterConfig selectedConfig;
    /// <summary>当前场景中的玩家控制器。</summary>
    private PlayerController2D currentPlayer;
    /// <summary>当前玩家成长组件。</summary>
    private PlayerProgression currentProgression;
    /// <summary>摄像机追随组件。</summary>
    private CameraFollow2D cameraFollow;
    /// <summary>当前场景刷怪器。</summary>
    private MonsterSpawner monsterSpawner;
    /// <summary>当前场景关卡管理器。</summary>
    private GameLevelManager levelManager;
    /// <summary>地图边界。</summary>
    private Bounds mapBounds;
    /// <summary>进入游戏场景时待加载的存档数据。</summary>
    private GameSaveData saveToLoad;

    /// <summary>创建游戏场景运行所需的玩家、刷怪、关卡和 UI 对象。</summary>
    private void Start()
    {
        saveToLoad = GameSaveStore.ConsumeLoadSavedGameRequest() ? GameSaveStore.SavedData : null;
        selectedConfig = GetSelectedConfig();
        mapBounds = GetMapBounds();

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
            cameraFollow = mainCamera.gameObject.AddComponent<CameraFollow2D>();

        monsterSpawner = gameObject.AddComponent<MonsterSpawner>();
        levelManager = gameObject.AddComponent<GameLevelManager>();

        PlayGameMusic();
        SpawnPlayer();
    }

    /// <summary>离开游戏场景时清理对象池和时间缩放。</summary>
    private void OnDestroy()
    {
        PoolMgr.Instance.ClearGameObjectPools();
        currentPlayer = null;
        currentProgression = null;
        Time.timeScale = 1f;
    }

    /// <summary>播放游戏背景音乐。</summary>
    private void PlayGameMusic()
    {
        MusicMgr.Instance.PlayBKMusic(GameMusicName);
    }

    /// <summary>根据选择的角色生成玩家并绑定战斗、UI、摄像机。</summary>
    private void SpawnPlayer()
    {
        if (selectedConfig == null)
        {
            Debug.LogError("No selected character config.");
            return;
        }

        ClearExistingPlayers();

        GameObject prefab = Resources.Load<GameObject>(selectedConfig.prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"Player prefab not found: Resources/{selectedConfig.prefabPath}.prefab");
            return;
        }

        GameObject playerObj = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        playerObj.name = selectedConfig.id;

        currentPlayer = playerObj.AddComponent<PlayerController2D>();
        PlayerRelicEffects relicEffects = playerObj.AddComponent<PlayerRelicEffects>();
        currentPlayer.Init(selectedConfig, mapBounds, OnPlayerDead);

        currentProgression = playerObj.AddComponent<PlayerProgression>();
        relicEffects.Init(currentPlayer, currentPlayer.RuntimeData, currentProgression);
        if (saveToLoad != null)
        {
            relicEffects.Restore(saveToLoad.relicData);
            currentPlayer.RuntimeData.Restore(saveToLoad.runtimeData);
            currentProgression.Restore(saveToLoad.progressionData);
        }
        PlayerCombat combat = playerObj.AddComponent<PlayerCombat>();

        monsterSpawner.Init(currentPlayer, currentProgression, mapBounds);
        combat.Init(currentPlayer, currentPlayer.RuntimeData, monsterSpawner);

        UIMgr.Instance.HidePanel<GamePanel>(true);
        UIMgr.Instance.ShowPanel<GamePanel>(E_UILayer.Middle, panel =>
        {
            panel.Bind(currentPlayer.RuntimeData, currentProgression);
            levelManager.Init(currentPlayer, currentProgression, monsterSpawner, panel, saveToLoad != null ? saveToLoad.stage : 1);
        }, true);

        if (cameraFollow != null)
            cameraFollow.Init(currentPlayer.CameraTarget, mapBounds);
    }

    /// <summary>玩家死亡时通知关卡管理器并解除摄像机目标。</summary>
    private void OnPlayerDead(PlayerController2D deadPlayer)
    {
        if (currentPlayer == deadPlayer)
        {
            currentPlayer = null;
            if (cameraFollow != null)
                cameraFollow.SetTarget(null);
        }

        levelManager?.OnPlayerDead(deadPlayer);
    }

    /// <summary>读取选角结果对应的角色配置，没有选择时使用第一个角色。</summary>
    private CharacterConfig GetSelectedConfig()
    {
        CharacterConfig[] configs = CharacterConfigLoader.LoadCharacters();
        if (configs.Length == 0)
            return null;

        string selectedId = SelectedCharacterData.SelectedCharacterId;
        if (saveToLoad != null && !string.IsNullOrEmpty(saveToLoad.characterId))
            selectedId = saveToLoad.characterId;

        if (!string.IsNullOrEmpty(selectedId))
        {
            foreach (CharacterConfig config in configs)
            {
                if (config.id == selectedId)
                    return config;
            }
        }

        return configs[0];
    }

    /// <summary>读取地图精灵边界。</summary>
    private Bounds GetMapBounds()
    {
        SpriteRenderer mapRenderer = GameObject.Find("Map1")?.GetComponent<SpriteRenderer>();
        return mapRenderer != null ? mapRenderer.bounds : new Bounds(Vector3.zero, Vector3.zero);
    }

    /// <summary>清理场景中手动放置或残留的玩家对象。</summary>
    private void ClearExistingPlayers()
    {
        foreach (PlayerController2D player in FindObjectsOfType<PlayerController2D>())
            Destroy(player.gameObject);

        DestroyManualPlayer("Player1");
        DestroyManualPlayer("Player2");
        DestroyManualPlayer("warrior");
        DestroyManualPlayer("archer");
    }

    /// <summary>按名字销毁手动放在场景里的玩家对象。</summary>
    private void DestroyManualPlayer(string objectName)
    {
        GameObject obj = GameObject.Find(objectName);
        if (obj != null)
            Destroy(obj);
    }
}
