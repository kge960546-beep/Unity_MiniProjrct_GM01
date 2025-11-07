using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MergeObject : MonoBehaviour
{
    Character Character;     
    DragController dragController;

    private MergeObject potentialMergeTarget;
    void Awake()
    {
        Character = GetComponent<Character>();
        dragController = GetComponent<DragController>();
    }
    public bool CanMergeWith(MergeObject other)
    {
        if (other == null || other.gameObject == gameObject) return false;
        if (Character == null || other.Character == null) return false;
        return other.Character.data.id == Character.data.id && other.Character.star == Character.star;
    }
    public bool ExecuteMerge(MergeObject target)
    {       
        if (this.GetInstanceID() < target.GetInstanceID()) 
        {           
            return false;
        }
        Transform mergeTile = target.transform.parent;
        if (mergeTile != null && mergeTile.CompareTag("Tile"))
        {
            return false;
        }

        int currentStar = Character.star;
        int nextIndex = currentStar;
        var data = Character.data;

        if (data == null || data.Prefabs == null || data.Prefabs.Length <= nextIndex || data.Prefabs[nextIndex] == null)
        {          
            return false;
        }
        GameObject nextPrefab = data.Prefabs[nextIndex];        
        Vector3 spawnPos = (target.transform.position + transform.position) * 0.5f;

        GameObject newUnitObj = Instantiate(nextPrefab, spawnPos, Quaternion.identity);
        if (mergeTile != null) newUnitObj.transform.SetParent(mergeTile, true);

        var newCs = newUnitObj.GetComponent<Character>();       

        if (newCs != null)
        {
            newCs.star = currentStar + 1;
            newCs.isEnemy = Character.isEnemy;
            newCs.ReSetState();
        }
        var newDc = newUnitObj.GetComponent<DragController>();
        if (newDc != null)
        {
            bool spawnFlag = (mergeTile != null && mergeTile.CompareTag("SpawnPoint"));
            newDc.isSpawnZone = spawnFlag;
            newDc.UpdatePositionAndParent();           
            newDc.ResetColliderState();           
        }
        Destroy(target.gameObject);
        Destroy(gameObject);

        return true;
    }  
}