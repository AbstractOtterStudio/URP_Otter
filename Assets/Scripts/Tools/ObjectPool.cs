using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//父級對象池
public class ObjectPool : MonoBehaviour
{
    public static ObjectPool instance { get; private set;}

    //所有子級對象池
    [Header("Setting")]
    public ObjectPoolSetting[] objPoolsSettings;

    static Dictionary<string, ObjectPoolInfo> poolInfo = new Dictionary<string, ObjectPoolInfo>();
    static Dictionary<GameObject, string> poolObjs = new Dictionary<GameObject, string>();

    public void Init()
    {
        if(instance == null) { instance = this; }   

        CreatePoolObject();
    }

    void CreatePoolObject()
    {
        foreach(var objPool in objPoolsSettings)
        {
            GameObject poolParent = new GameObject(objPool.name);
            poolParent.transform.SetParent(transform);
            poolInfo.Add(objPool.name, new ObjectPoolInfo(poolParent.transform, objPool.prefab, objPool.enableInPool));

            for(int i = 0; i < objPool.quantity; i++)
            {
                if(objPool.name == "Crab") { Debug.Log(objPool.quantity); }
                GameObject newObj = poolInfo[objPool.name].AddNewObj();
                poolObjs.Add(newObj, objPool.name);
            }
        }
    }

    //private void OnApplicationQuit() 
    //{
        //foreach(var poolSetting in objPoolsSettings)
        //{
        //    string poolName = poolSetting.name;
        //    int maxUse = poolInfo[poolName].maxOut;
        //    string objUsage = 
        //        poolInfo[poolName].addMoreCounter > 0 ||
        //        poolSetting.quantity - maxUse > 15 ?
        //        (maxUse + 15).ToString() : "-";
        //    Debug.Log($"Pool[{poolName}] max out value: {maxUse} ({objUsage})");
        //}
    //}

    public static Transform TakeFromPool(string pool)
    {
        Transform t = poolInfo[pool].Take();

        //if(poolInfo[pool].inObj < 10)
        //{
        //    instance.AddMore(pool);
        //}        
        return t;
    }

    //void AddMore(string pool)
    //{
    //    if(poolInfo[pool].corou == null)
    //    {
    //        poolInfo[pool].corou = StartCoroutine(AddMoreProcess(pool));
    //    }
    //}

    //IEnumerator AddMoreProcess(string pool)
    //{
    //    poolInfo[pool].addMoreCounter++;

    //    int addRate = (int)(poolInfo[pool].totalObj * 0.2f);
    //    if(addRate < 10)
    //    {
    //        addRate = 10;
    //    }

    //    for(int i = 0; i < addRate; i++)
    //    {
    //        GameObject newObj = poolInfo[pool].AddNewObj();
    //        poolObjs.Add(newObj, pool);
    //        yield return null;
    //    }
        
    //    poolInfo[pool].corou = null;
    //}

    public static void ReturnToPool(GameObject obj)
    {
        poolInfo[poolObjs[obj]].Return(obj);
    }
}

//對象池配置
[System.Serializable]
public struct ObjectPoolSetting
{
    public string name;
    public GameObject prefab;
    [Range(1, 30)] public int quantity;
    public bool enableInPool;
}

//具體對象池
public class ObjectPoolInfo
{
    Transform pool;
    GameObject prefab;
    readonly bool enableInPool;

    public int totalObj;
    public int outObj;
    public int inObj;
    //public int maxOut;
    //public int addMoreCounter;

    Dictionary<GameObject, bool> objList;
    public Coroutine corou;

    public ObjectPoolInfo(Transform pool, GameObject prefab, bool enableInPool)
    {
        this.pool = pool;
        this.prefab = prefab;
        this.enableInPool = enableInPool;

        //totalObj = outObj = inObj = maxOut = addMoreCounter = 0;
        objList = new Dictionary<GameObject, bool>();
    }

    public GameObject AddNewObj()
    {
        GameObject newObj = GameObject.Instantiate(prefab, pool.transform);
        newObj.SetActive(enableInPool);
        newObj.transform.position = pool.transform.position;
        newObj.name = $"{prefab.name} ({totalObj + 1})";
        objList.Add(newObj, false);
        totalObj++;
        inObj++;
        return newObj;
    }

    public Transform Take()
    {
        if(objList == null || objList.Count == 0 || inObj == 0)
        {
            return null;
        }

        Transform t = null;

        foreach(GameObject obj in objList.Keys)
        {
            if(!objList[obj])
            {
                outObj++;
                inObj--;
                objList[obj] = true;
                obj.SetActive(true);
                t = obj.transform;
                break;
            }
        }

        //if(outObj > maxOut)
        //{
        //    maxOut = outObj;
        //}

        return t;
    }

    public void Return(GameObject obj)
    {
        if(!objList.ContainsKey(obj))
        {
            return;
        }

        outObj--;
        inObj++;
        obj.SetActive(false);
        obj.transform.SetParent(pool);
        //obj.transform.SetPositionAndRotation(pool.position, pool.rotation);
        objList[obj] = false;
    }
}
