using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spwan : MonoBehaviour
{
    public Transform[] spawnPoints;
    public GameObject[] unitPrefabs;
    

    public void RandomUnitSpawn()
    {
        List<Transform> slots = new List<Transform>();
        foreach(Transform point in spawnPoints)
        {
            if(point.childCount == 0)
            {
                slots.Add(point);
            }            
        }
        if(slots.Count == 0)
        {
            Debug.Log("비어있는 슬롯이 없습니다");
            return;
        }
        //랜덤 유닛
        int randomUnitI = Random.Range(0, unitPrefabs.Length);
        GameObject unitSpwan = unitPrefabs[randomUnitI];
        //비어있는 슬롯 지정
        int randomSpawnI = Random.Range(0, slots.Count);
        Transform spawnPoint = slots[randomSpawnI];
        //슬롯 위치로 스폰
        GameObject newUnit = Instantiate(unitSpwan, spawnPoint.position, spawnPoint.rotation);
        newUnit.transform.SetParent(spawnPoint);
        DragController drgCon = newUnit.GetComponent<DragController>();
        if(drgCon != null)
        {
            drgCon.isSpawnZone = true;
            drgCon.myOriginalSpPaernt = spawnPoint;
        }        
    }   
}
