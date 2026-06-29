using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GameObject pool drawer for one prefab path.
/// </summary>
public class PoolData
{
    private readonly Stack<GameObject> dataStack = new Stack<GameObject>();
    private readonly List<GameObject> usedList = new List<GameObject>();
    private readonly int maxNum;
    private readonly GameObject rootObj;

    public int Count
    {
        get
        {
            CleanupDestroyedObjects();
            return dataStack.Count;
        }
    }

    public int UsedCount
    {
        get
        {
            CleanupDestroyedObjects();
            return usedList.Count;
        }
    }

    public bool NeedCreate
    {
        get
        {
            CleanupDestroyedObjects();
            return usedList.Count < maxNum;
        }
    }

    public PoolData(GameObject root, string name, GameObject usedObj)
    {
        if (PoolMgr.isOpenLayout && root != null)
        {
            rootObj = new GameObject(name);
            rootObj.transform.SetParent(root.transform);
        }

        PushUsedList(usedObj);

        PoolObj poolObj = usedObj != null ? usedObj.GetComponent<PoolObj>() : null;
        if (poolObj == null)
        {
            Debug.LogError("Please add PoolObj to pooled prefab.");
            maxNum = 30;
            return;
        }

        maxNum = poolObj.maxNum > 0 ? poolObj.maxNum : 30;
    }

    public GameObject Pop()
    {
        CleanupDestroyedObjects();

        GameObject obj = null;
        if (dataStack.Count > 0)
        {
            obj = dataStack.Pop();
        }
        else if (usedList.Count > 0)
        {
            obj = usedList[0];
            usedList.RemoveAt(0);
        }

        if (obj == null)
            return null;

        usedList.Add(obj);
        obj.SetActive(true);
        if (PoolMgr.isOpenLayout)
            obj.transform.SetParent(null);

        return obj;
    }

    public void Push(GameObject obj)
    {
        if (obj == null)
            return;

        CleanupDestroyedObjects();
        obj.SetActive(false);
        if (PoolMgr.isOpenLayout && rootObj != null)
            obj.transform.SetParent(rootObj.transform);

        dataStack.Push(obj);
        usedList.Remove(obj);
    }

    public void PushUsedList(GameObject obj)
    {
        if (obj != null)
            usedList.Add(obj);
    }

    public void CleanupDestroyedObjects()
    {
        usedList.RemoveAll(obj => obj == null);

        if (dataStack.Count <= 0)
            return;

        List<GameObject> aliveObjs = new List<GameObject>();
        foreach (GameObject obj in dataStack)
        {
            if (obj != null)
                aliveObjs.Add(obj);
        }

        dataStack.Clear();
        for (int i = aliveObjs.Count - 1; i >= 0; --i)
            dataStack.Push(aliveObjs[i]);
    }

    public void ClearAll()
    {
        foreach (GameObject obj in dataStack)
        {
            if (obj != null)
                GameObject.Destroy(obj);
        }

        for (int i = 0; i < usedList.Count; ++i)
        {
            if (usedList[i] != null)
                GameObject.Destroy(usedList[i]);
        }

        dataStack.Clear();
        usedList.Clear();
    }
}

public abstract class PoolObjectBase
{
}

public class PoolObject<T> : PoolObjectBase where T : class
{
    public Queue<T> poolObjs = new Queue<T>();
}

public interface IPoolObject
{
    void ResetInfo();
}

/// <summary>
/// Shared object pool manager.
/// </summary>
public class PoolMgr : BaseManager<PoolMgr>
{
    private readonly Dictionary<string, PoolData> poolDic = new Dictionary<string, PoolData>();
    private readonly Dictionary<string, PoolObjectBase> poolObjectDic = new Dictionary<string, PoolObjectBase>();
    private GameObject poolObj;

    public static bool isOpenLayout = true;

    private PoolMgr()
    {
        EnsurePoolRoot();
    }

    private void EnsurePoolRoot()
    {
        if (!isOpenLayout)
            return;

        if (poolObj != null)
            return;

        poolObj = new GameObject("Pool");
        GameObject.DontDestroyOnLoad(poolObj);
        poolDic.Clear();
    }

    public void ClearGameObjectPools()
    {
        foreach (PoolData poolData in poolDic.Values)
            poolData.ClearAll();

        if (poolObj != null)
            GameObject.Destroy(poolObj);

        poolObj = null;
        poolDic.Clear();
    }

    public GameObject GetObj(string name)
    {
        EnsurePoolRoot();

        GameObject obj;
        if (poolDic.ContainsKey(name))
            poolDic[name].CleanupDestroyedObjects();

        if (!poolDic.ContainsKey(name) || (poolDic[name].Count == 0 && poolDic[name].NeedCreate))
        {
            obj = CreatePooledObj(name);
            if (!poolDic.ContainsKey(name))
                poolDic.Add(name, new PoolData(poolObj, name, obj));
            else
                poolDic[name].PushUsedList(obj);
        }
        else
        {
            obj = poolDic[name].Pop();
            if (obj == null)
            {
                obj = CreatePooledObj(name);
                poolDic[name].PushUsedList(obj);
            }
        }

        return obj;
    }

    public T GetObj<T>(string nameSpace = "") where T : class, IPoolObject, new()
    {
        string poolName = nameSpace + "_" + typeof(T).Name;
        if (poolObjectDic.ContainsKey(poolName))
        {
            PoolObject<T> pool = poolObjectDic[poolName] as PoolObject<T>;
            if (pool != null && pool.poolObjs.Count > 0)
                return pool.poolObjs.Dequeue();
        }

        return new T();
    }

    public void PushObj(GameObject obj)
    {
        if (obj == null)
            return;

        if (!poolDic.ContainsKey(obj.name))
        {
            GameObject.Destroy(obj);
            return;
        }

        poolDic[obj.name].Push(obj);
    }

    public void PushObj<T>(T obj, string nameSpace = "") where T : class, IPoolObject
    {
        if (obj == null)
            return;

        string poolName = nameSpace + "_" + typeof(T).Name;
        if (!poolObjectDic.ContainsKey(poolName))
            poolObjectDic.Add(poolName, new PoolObject<T>());

        obj.ResetInfo();
        (poolObjectDic[poolName] as PoolObject<T>)?.poolObjs.Enqueue(obj);
    }

    private GameObject CreatePooledObj(string name)
    {
        GameObject prefab = Resources.Load<GameObject>(name);
        if (prefab == null)
        {
            Debug.LogError($"Pool prefab not found: Resources/{name}.prefab");
            return null;
        }

        GameObject obj = GameObject.Instantiate(prefab);
        obj.name = name;
        return obj;
    }
}
