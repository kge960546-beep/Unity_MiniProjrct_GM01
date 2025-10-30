using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public bool[,] map;
    public List<Vector3Int> wallPositions;
    public int width = 20;
    public int height = 20;

    private void Awake()
    {
        Instance = this;
        map = Node.ConvertToMap(wallPositions, width, height);        
    }   
}
