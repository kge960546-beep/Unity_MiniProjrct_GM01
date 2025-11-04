using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    [Header("골드시스템")]
    [SerializeField] private int playerGold = 100;
    public int goldConsumption = 20;
    public event Action<int> OnGoldChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);            
        }
        else { Destroy(gameObject); return; }
        InitializeMap();

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
    void InitializeMap()
    {
        if (mapData == null)
        {
            Debug.LogError("[GameManager] mapData == null X");
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
                if (!src[x, y]) // 벽
                {
                    for (int dx = -padding; dx <= padding; dx++)
                    {
                        for (int dy = -padding; dy <= padding; dy++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx >= 0 && nx < w && ny >= 0 && ny < h)
                                dst[nx, ny] = false; // 주변도 벽으로 간주
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

}
