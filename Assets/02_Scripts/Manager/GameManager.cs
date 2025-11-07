using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("¸Ê ¼³Á¤")]
    public int width = 40;
    public int height = 40;

    [Header("º® À§Ä¡ ¸®½ºÆ®")]
    public List<Vector3Int> wallPositions;

    [Header("ÀüÀå ¿µ¿ª")]
    public Vector2Int battleMapMin = new Vector2Int(-14, -3);
    public Vector2Int battleMapMax = new Vector2Int(14, 5);

    public bool[,] map;
    public Vector2Int mapOffset;

    [Header("¾À ¸Ê µ¥ÀÌÅÍ")]
    public MapData mapData;

    [Header("°ñµå&¸ñ¼û½Ã½ºÅÛ")]
    [SerializeField] public int playerGold = 100;
    public int goldConsumption = 20;
    public event Action<int> OnGoldChanged;
        public int lifeCount = 3;
    public int maxLife = 3;

    [Header("À¯´Ö µ¥ÀÌÅÍ °ü¸®")]
    public List<PlayerUnitData> saveUnits = new List<PlayerUnitData>();
    public GameObject[] allUnitPrefabs;
    private Dictionary<string, GameObject> unitPrefabDict = new Dictionary<string, GameObject>();
    public bool saveUnitsEnabled = false;

    //JSON
    string savePath => Path.Combine(Application.persistentDataPath, "saveData.json");
    public bool restoreOnLoad = false;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }      

        unitPrefabDict.Clear();
        if(allUnitPrefabs != null)
        {
            foreach(var p in allUnitPrefabs)
            {
                if (p == null) continue;
                if(!unitPrefabDict.ContainsKey(p.name))
                {
                    unitPrefabDict.Add(p.name, p);
                }
            }
        }
        
        Physics2D.autoSyncTransforms = true;
    }
    public bool SpendGold(int amountSpend)
    {       
        if (playerGold >= amountSpend)
        {
            playerGold -= amountSpend;
            OnGoldChanged?.Invoke(playerGold);
            return true;
        }
        else
        {
            return false;
        }
    }
    public void EarnGold(int amountEarn)
    {
        if(amountEarn > 0)
        {
            playerGold += amountEarn;
            OnGoldChanged?.Invoke(playerGold);
        }        
    }
    public void ResetGameData()
    {
        playerGold = 100;
        lifeCount = maxLife;
        saveUnits.Clear();

        if (File.Exists(savePath))
        {
            File.Delete(savePath);                   
        }
    }
    [System.Serializable]
    public class PlayerUnitData
    {
        public string unitName;
        public Vector3 position;
        public int star;
        public bool isEnemy;
        public int instanceID;
        public PlayerUnitData(string name, Vector3 pos, int starLevel, bool enemy, int instanceID)
        {
            unitName = name;
            position = pos;
            star = starLevel;
            isEnemy = enemy;
            this.instanceID = instanceID;
        }
    }
    [System.Serializable]
    public class SavePayload
    {      
        public List<PlayerUnitData> saveUnits;
    }
    public void SnapshotUnitsFromScene()
    {
        saveUnits.Clear();

        var units = GameObject.FindObjectsOfType<Character>(true);
        foreach (var cs in units)
        {
            if (cs.isEnemy) continue; 
            if (cs.isDead) continue;
            var go = cs.gameObject;
            string cleanName = go.name
                .Replace("(Clone)", "")
                .Replace("(1)", "")
                .Replace("(2)", "")
                .Replace("(3)", "")
                .Replace(" Variant", "")
                .Trim();

            Vector3 savePos = go.transform.position;
            if (go.transform.parent != null)
            {
                savePos = go.transform.parent.position;
            }

            saveUnits.Add(new PlayerUnitData(
                cleanName,
                savePos,
                cs.star,
                cs.isEnemy,
                0
                ));
        }
    }    
    public void SaveToJson()
    {
        saveUnits.RemoveAll(u => u == null);

        var payload = new SavePayload
        {           
            saveUnits = new List<PlayerUnitData>(saveUnits)
        };

        string json = JsonUtility.ToJson(payload, true);
        File.WriteAllText(savePath, json);
    }
    public bool LoadFromJson()
    {
        if (!File.Exists(savePath))
        {
            return false;
        }

        string json = File.ReadAllText(savePath);
        var payload = JsonUtility.FromJson<SavePayload>(json);
        if (payload == null)
        {
            return false;
        }        
        saveUnits = payload.saveUnits ?? new List<PlayerUnitData>();

        return true;
    }
    public void DestroyAllUnitsInScene()
    {
        var units = GameObject.FindObjectsOfType<Character>(true);
        foreach (var cs in units)
        {            
            if (!cs.isEnemy)
            {
                Destroy(cs.gameObject);
            }
        }
        Physics2D.SyncTransforms();
    }
    public void LoadFromJsonAndRestore()
    {
        if (!LoadFromJson()) return;

        DestroyAllUnitsInScene();

        saveUnitsEnabled = true;
        RestoreUnits();         

        Physics2D.SyncTransforms();
    }

    public void RestoreUnits()
    {
        if (!saveUnitsEnabled) return;
        List<GameObject> allTiles = new List<GameObject>();
        allTiles.AddRange(GameObject.FindGameObjectsWithTag("SpawnPoint"));
        allTiles.AddRange(GameObject.FindGameObjectsWithTag("Tile"));

        foreach (var data in saveUnits)
        {
            GameObject prefab = null;
           
            if (!unitPrefabDict.TryGetValue(data.unitName, out prefab))
            {               
                string fallbackName = data.unitName.Replace("(Clone)", "").Replace(" Variant", "").Trim();

                foreach (var kvp in unitPrefabDict)
                {
                    if (kvp.Key.Equals(fallbackName, StringComparison.OrdinalIgnoreCase))
                    {
                        prefab = kvp.Value;
                        break;
                    }
                }

                if (prefab == null)
                {
                    Debug.LogWarning($"[RestoreUnits] ÇÁ¸®ÆÕ Ã£±â ½ÇÆÐ: {data.unitName} / fallback={fallbackName}");
                    continue;
                }
            }
            GameObject unit = Instantiate(prefab, data.position, Quaternion.identity);
            unit.SetActive(true);
            Character cs = unit.GetComponent<Character>();

            if (cs != null)
            {
                cs.star = data.star;
                cs.isEnemy = data.isEnemy;
                cs.presentHP = cs.data.HPMax;        
                cs.isDead = false;       
                cs.gameObject.SetActive(true);
                cs.Upgrade();
                cs.ReSetState();
            }

            Transform parentTile = null;
            float closestDist = 0.6f;

            foreach (var tile in allTiles)
            {
                float dist = Vector3.Distance(tile.transform.position, unit.transform.position);
                if (dist < closestDist)
                {
                    parentTile = tile.transform;
                    closestDist = dist;
                }
            }

            if (parentTile != null)
                unit.transform.SetParent(parentTile);
            
            DragController dc = unit.GetComponent<DragController>();
            if (dc != null)
            {
                dc.isSpawnZone = (parentTile != null && parentTile.CompareTag("SpawnPoint"));
                dc.UpdatePositionAndParent();
                dc.ResetColliderState();
            }
        }

        Physics2D.SyncTransforms();       
    }    
    public void RestartStageKeepBoard()
    {
        SnapshotUnitsFromScene();
        SaveToJson();
        restoreOnLoad = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    void OnSceneLoaded(Scene scene,LoadSceneMode mode)
    {
        Debug.Log($"[GameManager] ¾À ·ÎµåµÊ: {scene.name}, restoreOnLoad={restoreOnLoad}");
        MapLoader loader = FindObjectOfType<MapLoader>();
        if (loader != null && loader.mapData != null)
        {           
            mapData = loader.mapData;
        }
        else
        {            
            mapData = null;
        }
        InitializeMap();
        StartCoroutine(DelayedRestore());
    }
    private IEnumerator DelayedRestore()
    {        
        yield return null;
        yield return null;

        if (restoreOnLoad)
        {
            restoreOnLoad = false;
            saveUnitsEnabled = true;
            LoadFromJsonAndRestore();
        }
    }
    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }
    public void InitializeMap()
    {
        if (mapData == null)
        {           
            return;
        }

        width = mapData.width;
        height = mapData.height;
        wallPositions = new List<Vector3Int>(mapData.wallPos);
        mapOffset = new Vector2Int(width / 2, height / 2);
        map = Node.ConvertToMap(wallPositions, width, height);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {                
                Vector2Int worldPos = new Vector2Int(x - mapOffset.x, y - mapOffset.y);
                
                if (!IsInsideBattle(worldPos))
                {                    
                    map[x, y] = false;
                }
            }
        }
        map = InflateWalls(map, 1);
        ValidateMap();
    }
    bool[,] InflateWalls(bool[,] src, int padding)
    {
        int w = src.GetLength(0), h = src.GetLength(1);
        bool[,] dst = (bool[,])src.Clone();
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                if (!src[x, y]) //º®
                {
                    for (int dx = -padding; dx <= padding; dx++)
                    {
                        for (int dy = -padding; dy <= padding; dy++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx >= 0 && nx < w && ny >= 0 && ny < h)
                                dst[nx, ny] = false;
                        }
                    }
                }
            }
        return dst;
    }
    void ValidateMap()
    {
        if (map == null) { return; }

        int walkableCount = 0;
        int blockedCount = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (map[x, y])
                    walkableCount++;
                else
                    blockedCount++;
            }
        }
    }
    public bool IsInsideBattle(Vector2Int worldPos)
    {
        return worldPos.x >= battleMapMin.x && worldPos.x <= battleMapMax.x &&
               worldPos.y >= battleMapMin.y && worldPos.y <= battleMapMax.y;
    }
    private void OnDrawGizmosSelected()
    {
        if (map == null) return;
        
        Gizmos.color = Color.red;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!map[x, y]) 
                {                    
                    Vector3 pos = new Vector3(
                        x - mapOffset.x + 0.5f,
                        y - mapOffset.y + 0.5f,
                        0);
                    Gizmos.DrawCube(pos, Vector3.one * 0.2f);
                }
            }
        }
        
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3(
            (battleMapMin.x + battleMapMax.x) / 2f,
            (battleMapMin.y + battleMapMax.y) / 2f,
            0);
        Vector3 size = new Vector3(
            battleMapMax.x - battleMapMin.x,
            battleMapMax.y - battleMapMin.y,
            0);
        Gizmos.DrawWireCube(center, size);
        
        Gizmos.color = Color.cyan;
        Vector3 mapCenter = new Vector3(-mapOffset.x + width / 2f, -mapOffset.y + height / 2f, 0);
        Vector3 mapSize = new Vector3(width, height, 0);
        Gizmos.DrawWireCube(mapCenter, mapSize);
    }
    public GameObject GetPrefabByName(string prefabName)
    {       
        foreach (var prefab in allUnitPrefabs)
        {           
            if (prefab.name == prefabName)
            {              
                return prefab;
            }
        }        
        return null;
    }
}
