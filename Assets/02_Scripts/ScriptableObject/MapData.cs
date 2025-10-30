using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName ="newMapData",menuName ="MapData")]
public class MapData : ScriptableObject
{
    [Header("맵크기")]
    public int width = 40;
    public int height = 40;

    [Header("장애물 위치")]
    public List<Vector3Int> wallPos;
}
