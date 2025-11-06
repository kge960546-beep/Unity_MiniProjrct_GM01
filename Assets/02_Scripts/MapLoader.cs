using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapLoader : MonoBehaviour
{
    public MapData mapData;

    private void Start()
    {
        if(GameManager.Instance != null)
        {
            GameManager.Instance.mapData = mapData;
            GameManager.Instance.InitializeMap();
        }
    }
}
