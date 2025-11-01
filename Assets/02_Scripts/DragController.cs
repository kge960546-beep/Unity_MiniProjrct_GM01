using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using static UnityEditorInternal.ReorderableList;

public class DragController : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    Vector2 DefaultPos;
    public bool isSpawnZone;

    SpriteRenderer sr;
    Collider2D col;
    Character thisChar;

    private Transform originalSpParent;
    public Transform myOriginalSpPaernt;
    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        thisChar = GetComponent<Character>();
    }
   
    void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
    {
        DefaultPos = transform.position;
        originalSpParent = myOriginalSpPaernt; // 원래 부모(SpawnPoint 등)를 기억
        transform.SetParent(null); //// 드래그 동안 최상위로 이동하여 자유롭게 움직임

        sr.color = new Color(1.0f, 1.0f, 1.0f, 0.6f);
        col.isTrigger = true;       
        col.enabled = false;
    }
    void IDragHandler.OnDrag(PointerEventData eventData)
    {
        Vector3 currentPos = Camera.main.ScreenToWorldPoint(eventData.position);
        currentPos.z = 0;
        transform.position = currentPos;
    }
    void IEndDragHandler.OnEndDrag(PointerEventData eventData)
    {        
        Character otherCharacter = FindMergeableCharacterAt(transform.position);

        if (otherCharacter != null)
        {            
            if (TryMerge(otherCharacter))
            {
                return;
            }
        }
        PlaceOnTile(eventData.position);
        ResetToDefaultState();

        sr.color = Color.white;
        col.isTrigger = false;
        col.enabled = true;
    }
    private Character FindMergeableCharacterAt(Vector2 pos)
    {
        col.enabled = true;
        Collider2D[] hitCol = Physics2D.OverlapCircleAll(pos, 0.4f);
        col.enabled = false;

        foreach(var hitCollider in hitCol)
        {
            if (hitCollider.gameObject == this.gameObject) continue; // 자기 자신은 제외

            Character targetChar = hitCollider.GetComponent<Character>();
            // 상대방이 Character 컴포넌트를 가지고 있고, ID와 Star가 모두 같다면
            if (targetChar != null &&
                targetChar.data.id == this.thisChar.data.id &&
                targetChar.star == this.thisChar.star)
            {
                return targetChar; // 병합할 대상 캐릭터를 반환
            }
        }
        return null; // 병합 대상을 찾지 못함
    }
    
    private bool TryMerge(Character otherChar)
    {
        int currentStar = thisChar.star;
        int nextPrefabIndex = currentStar;

        // 다음 등급의 프리팹이 CharacterData에 설정되어 있는지 확인
        if (thisChar.data.Prefabs.Length > nextPrefabIndex && thisChar.data.Prefabs[nextPrefabIndex] != null)
        {
            Transform mergeTile = otherChar.transform.parent;

            GameObject newUnitObj = Instantiate(thisChar.data.Prefabs[nextPrefabIndex], mergeTile.position, Quaternion.identity);
            newUnitObj.transform.SetParent(mergeTile);

            // 다음 등급 유닛을 상대방 캐릭터 위치에 생성
            Character newUnitCharacter = newUnitObj.GetComponent<Character>();
            DragController newUnitDragCon = newUnitObj.GetComponent<DragController>();

            if (newUnitCharacter != null && newUnitDragCon != null)
            {
                newUnitCharacter.star = currentStar + 1; // 새 유닛의 star 등급 설정               

                newUnitDragCon.isSpawnZone = true;
                newUnitCharacter.ReSetState();
            }
            

            // 병합에 사용된 두 유닛 파괴
            Destroy(otherChar.gameObject);
            Destroy(this.gameObject);
            return true;
        }
        else
        {
            return false;
        }
    }
    private void PlaceOnTile(Vector2 screenPos)
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(screenPos);
        mousePos.z = 0;

        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
        Transform targetTile = null;

        if(hit.collider != null)
        {
            if (hit.collider.CompareTag("Tile") || hit.collider.CompareTag("SpawnPoint"))
            {
                targetTile = hit.collider.transform;
            }
        }
        if (targetTile != null && targetTile.childCount == 0)
        {
            transform.position = targetTile.position;
            transform.SetParent(targetTile);
            myOriginalSpPaernt = targetTile;
            isSpawnZone = targetTile.CompareTag("SpawnPoint");
            thisChar?.ReSetState();
        }
        else
        {
            transform.position = DefaultPos;
            transform.SetParent(myOriginalSpPaernt);
        }
    }
    private void ResetToDefaultState()
    {
        sr.sortingOrder = 5; // 원래 sortingOrder로 복구
        sr.color = Color.white;
        col.isTrigger = false;
        col.enabled = true;
    }
}
