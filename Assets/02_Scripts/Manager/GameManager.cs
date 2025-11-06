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

    [Header("맵 설정")]
    public int width = 40;
    public int height = 40;

    [Header("벽 위치 리스트")]
    public List<Vector3Int> wallPositions;

    [Header("전장 영역")]
    public Vector2Int battleMapMin = new Vector2Int(-14, -3);
    public Vector2Int battleMapMax = new Vector2Int(14, 5);

    public bool[,] map;
    public Vector2Int mapOffset;

    [Header("씬 맵 데이터")]
    public MapData mapData;

    [Header("골드&목숨시스템")]
    [SerializeField] public int playerGold = 100;
    public int goldConsumption = 20;
    public event Action<int> OnGoldChanged;
        public int lifeCount = 3;
    public int maxLife = 3;

    [Header("유닛 데이터 관리")]
    public List<PlayerUnitData> saveUnits = new List<PlayerUnitData>();
    public GameObject[] allUnitPrefabs;
    private Dictionary<string, GameObject> unitPrefabDict = new Dictionary<string, GameObject>();
    public bool saveUnitsEnabled = false;

    //JSON
    string savePath => Path.Combine(Application.persistentDataPath, "saveData.json");

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

        InitializeMap();
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
            Debug.Log("골드가 부족합니다");
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
        public int playerGold;
        public int lifeCount;
        public List<PlayerUnitData> saveUnits;
    }
    public void SnapshotUnitsFromScene()
    {
        saveUnits.Clear();

        var units = GameObject.FindObjectsOfType<Character>();
        foreach (var cs in units)
        {
            var go = cs.gameObject;
            string cleanName = go.name.Replace("(Clone)", "").Trim();

            // instanceID는 세션마다 바뀌어도 OK (복원 후 새 ID로 갱신 안 해도 JSON 로직엔 영향 없음)
            saveUnits.Add(new PlayerUnitData(
                cleanName,
                go.transform.position,
                cs.star,
                cs.isEnemy,
                0
            ));
        }

        Debug.Log($"[SnapshotUnitsFromScene] 스냅샷 완료: {saveUnits.Count}개");
    }
    // ======= JSON으로 디스크 저장 =======
    public void SaveToJson()
    {
        var payload = new SavePayload
        {
            playerGold = playerGold,
            lifeCount = lifeCount,
            saveUnits = new List<PlayerUnitData>(saveUnits)
        };

        string json = JsonUtility.ToJson(payload, true);
        File.WriteAllText(savePath, json);
        Debug.Log($"[SaveToJson] 저장 완료: {savePath}");
    }
    public bool LoadFromJson()
    {
        if (!File.Exists(savePath))
        {
            Debug.LogWarning($"[LoadFromJson] 저장 파일 없음: {savePath}");
            return false;
        }

        string json = File.ReadAllText(savePath);
        var payload = JsonUtility.FromJson<SavePayload>(json);
        if (payload == null)
        {
            Debug.LogError("[LoadFromJson] 파싱 실패");
            return false;
        }

        playerGold = payload.playerGold;
        lifeCount = payload.lifeCount;
        saveUnits = payload.saveUnits ?? new List<PlayerUnitData>();

        Debug.Log($"[LoadFromJson] 로드 완료: 유닛 {saveUnits.Count}개");
        return true;
    }
    public void DestroyAllUnitsInScene()
    {
        var units = GameObject.FindObjectsOfType<Character>();
        foreach (var cs in units)
            Destroy(cs.gameObject);

        Physics2D.SyncTransforms();
        Debug.Log("[DestroyAllUnitsInScene] 씬 유닛 제거 완료");
    }
    public void LoadFromJsonAndRestore()
    {
        if (!LoadFromJson()) return;

        DestroyAllUnitsInScene(); // 중복 방지

        bool prev = saveUnitsEnabled;
        saveUnitsEnabled = true;  //  일시적으로 가드 해제
        RestoreUnits();           // 저장본 복원
        saveUnitsEnabled = prev;

        Physics2D.SyncTransforms();
        Debug.Log("[LoadFromJsonAndRestore] 복원 완료");
    }

    public void RestoreUnits()
    {
        if (!saveUnitsEnabled) return;
        List<GameObject> allTiles = new List<GameObject>();
        allTiles.AddRange(GameObject.FindGameObjectsWithTag("SpawnPoint"));
        allTiles.AddRange(GameObject.FindGameObjectsWithTag("Tile"));

        foreach (var data in saveUnits)
        {
            if (unitPrefabDict.TryGetValue(data.unitName, out GameObject prefab))
            {
                GameObject unit = Instantiate(prefab, data.position, Quaternion.identity);
                Character cs = unit.GetComponent<Character>();

                if (cs != null)
                {
                    cs.star = data.star;
                    cs.isEnemy = data.isEnemy;
                    cs.Upgrade();
                    cs.ReSetState();
                }

                // 부모 타일 찾아주기
                Transform parentTile = null;
                float closestDist = 0.2f;

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
                {
                    unit.transform.SetParent(parentTile);
                }

                DragController dc = unit.GetComponent<DragController>();
                if (dc != null)
                {
                    dc.isSpawnZone = (parentTile != null && parentTile.CompareTag("SpawnPoint"));
                    dc.UpdatePositionAndParent();
                    dc.ResetColliderState(); // 콜라이더 상태 명시적 초기화
                }
            }
        }
    }
    public void RestartStageKeepBoard()
    {
        SnapshotUnitsFromScene();
        SaveToJson();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    void OnSceneLoaded(Scene scene,LoadSceneMode mode)
    {
        mapData = FindObjectOfType<MapData>();
        InitializeMap();
        LoadFromJsonAndRestore();
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

        Debug.Log($"[GameManager] wallPositions.Count = {wallPositions.Count}");
        foreach (var wall in wallPositions)
        {
            Debug.Log($"[GameManager] wall pos = {wall}");
        }
        map = Node.ConvertToMap(wallPositions, width, height);

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
                if (!src[x, y]) //벽
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
