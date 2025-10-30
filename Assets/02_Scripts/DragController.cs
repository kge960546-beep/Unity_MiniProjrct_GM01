using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragController : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    Vector2 DefaultPos;    
    public bool isSpawnZone;
    SpriteRenderer sr;
    Collider2D col;

    public Transform spawnPoints;
    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }
    void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
    {        
        DefaultPos = transform.position;

        sr.color = new Color(1.0f, 1.0f, 1.0f, 0.6f);
        col.isTrigger = true;
        col.enabled = false;

        transform.SetParent(null);
    }
    void IDragHandler.OnDrag(PointerEventData eventData)
    {
        Vector3 currentPos = Camera.main.ScreenToWorldPoint(eventData.position);      
        currentPos.z = 0;
        transform.position = currentPos;        
    }
    void IEndDragHandler.OnEndDrag(PointerEventData eventData)
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(eventData.position);
        mousePos.z = 0;

        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

        if(hit.collider != null && hit.collider.CompareTag("Tile"))
        {
            transform.position = hit.collider.transform.position;
            isSpawnZone = false;
        }
        else if(hit.collider != null && hit.collider.CompareTag("SpawnPoint"))
        {
            transform.position = hit.collider.transform.position;
            transform.SetParent(hit.collider.transform);
            isSpawnZone = true;

            Character character = GetComponent<Character>();
            if(character != null)
            {
                character.ReSetState();
            }
        }       
        else
        {
            transform.position = DefaultPos;
            transform.SetParent(spawnPoints);
            isSpawnZone = true;
        }

        sr.color = Color.white;
        col.isTrigger = false;
        col.enabled = true;
    }  
}
