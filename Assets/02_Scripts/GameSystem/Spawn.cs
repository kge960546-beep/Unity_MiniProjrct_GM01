using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawn : MonoBehaviour
{
    public Transform[] spawnPoints;
    public GameObject[] unitPrefabs;   

    public int summonCost = 20;   
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
            return;
        }
        //·£´ý À¯´Ö
        int randomUnitI = Random.Range(0, unitPrefabs.Length);
        GameObject unitSpwan = unitPrefabs[randomUnitI];
        //ºñ¾îÀÖ´Â ½½·Ô ÁöÁ¤
        int randomSpawnI = Random.Range(0, slots.Count);
        Transform spawnPoint = slots[randomSpawnI];
        if(GameManager.Instance.SpendGold(summonCost))
        {
            //½½·Ô À§Ä¡·Î ½ºÆù
            GameObject newUnit = Instantiate(unitSpwan, spawnPoint.position, spawnPoint.rotation);
            newUnit.transform.SetParent(spawnPoint);
            DragController drgCon = newUnit.GetComponent<DragController>();
            if (drgCon != null)
            {
                drgCon.isSpawnZone = true;
                drgCon.myOriginalSpPaernt = spawnPoint;
            }
        }
        else
        {
            return;
        }
        
       
    }   
}
