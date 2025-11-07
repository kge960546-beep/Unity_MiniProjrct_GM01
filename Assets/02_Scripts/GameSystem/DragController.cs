using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
public class DragController : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    Vector2 defaultPos;
    Transform originalParent;

    SpriteRenderer sr;
    Collider2D col;
    MergeObject merge;

    public bool isDragging;
    int originalLayer;
    Color originalColor;
    int originalSortingOrder;

    public bool isSpawnZone;
    const string IgnoreLayerName = "Ignore Raycast";
    int ignoreLayer;
    Camera dragCam;
    private bool hasInitialized = false;
    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        merge = GetComponent<MergeObject>();
        originalLayer = gameObject.layer;
        originalColor = sr != null ? sr.color : Color.white;
        originalSortingOrder = sr != null ? sr.sortingOrder : 0;
        ignoreLayer = LayerMask.NameToLayer(IgnoreLayerName);

        if (col != null)
        {
            col.enabled = true;
            col.isTrigger = false;
        }
    }
    public void UpdatePositionAndParent()
    {
        defaultPos = transform.position;
        originalParent = transform.parent;
        hasInitialized = true;
    }
    public void ResetColliderState()
    {
        if (col != null)
        {
            col.enabled = true;
            col.isTrigger = false;
        }
        gameObject.layer = originalLayer;
    }
    Vector3 ScreenToWorld(Camera cam, Vector2 screenPos)
    {
        if (cam == null) cam = Camera.main;
        Vector3 wp = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0.0f));
        wp.z = 0.0f;
        return wp;
    }

    bool ScreenPosOnUnit(Vector2 screenPos)
    {
        if (col == null)
        {
            return false;
        }
        
        bool wasEnabled = col.enabled;
        if (!wasEnabled)
        {
            col.enabled = true;
        }

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
        Vector2 worldPos2D = new Vector2(worldPos.x, worldPos.y);
        bool result = col.OverlapPoint(worldPos2D);

        if (!wasEnabled)
        {
            col.enabled = false;
        }
        return result;
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!hasInitialized) UpdatePositionAndParent();

        dragCam = eventData.pressEventCamera != null ? eventData.pressEventCamera : Camera.main;
        
        if (!ScreenPosOnUnit(eventData.pressPosition))
        {
            eventData.pointerDrag = null;
            isDragging = false;
            return;
        }

        isDragging = true;
        defaultPos = transform.position;
        originalParent = transform.parent;

        transform.SetAsLastSibling();

        if (sr != null)
        {
            sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.4f);
            sr.sortingOrder = originalSortingOrder + 100;
        }

        if (col != null)
        {
            col.enabled = true;
            col.isTrigger = true;
        }

        if (ignoreLayer >= 0)
        {
            gameObject.layer = ignoreLayer;
        }
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        transform.position = ScreenToWorld(dragCam, eventData.position);
    }
    public void OnEndDrag(PointerEventData eventData)
    {        
        if (col != null)
        {            
            col.enabled = false;
        }

        try
        {
            MergeObject potentialMergeTarget = null;
            Transform potentialEmptyTile = null;
            Vector3 wp = ScreenToWorld(dragCam, eventData.position);
            Collider2D[] hits = Physics2D.OverlapCircleAll(new Vector2(wp.x, wp.y), 0.2f);

            foreach (var h in hits)
            {
                MergeObject otherMerge = h.GetComponent<MergeObject>();
                if (merge != null && merge.CanMergeWith(otherMerge))
                {
                    potentialMergeTarget = otherMerge;
                }

                if (h.CompareTag("Tile") || h.CompareTag("SpawnPoint"))
                {
                    if (h.transform.childCount == 0)
                    {
                        potentialEmptyTile = h.transform;                        
                    }
                }
            }
           
            if (potentialMergeTarget != null)
            {
                MergeObject me = merge;
                MergeObject other = potentialMergeTarget;
                MergeObject leader = (me.GetInstanceID() >= other.GetInstanceID()) ? me : other;
                MergeObject follower = (leader == me) ? other : me;

                if (leader.ExecuteMerge(follower))
                    return;
            }
            if (potentialEmptyTile != null)
            {
                PlaceOnTile(potentialEmptyTile);
            }
            else
            {
                ReturnToDefaultPosition();
            }
        }
        finally
        {
            if (this != null && gameObject != null)
            {
                ResetToDefaultState();
            }
            isDragging = false;
            dragCam = null;
        }
    }
    void PlaceOnTile(Transform targetTile)
    {
        transform.position = targetTile.position;
        transform.SetParent(targetTile, true);
        isSpawnZone = targetTile.CompareTag("SpawnPoint");
        defaultPos = transform.position;
        originalParent = transform.parent;
    }

    void ReturnToDefaultPosition()
    {
        transform.position = defaultPos;
        transform.SetParent(originalParent, true);
    }
    void ResetToDefaultState()
    {
        if (sr != null)
        {
            sr.color = originalColor;
            sr.sortingOrder = originalSortingOrder;
        }

        if (col != null)
        {
            col.enabled = true;
            col.isTrigger = false;
        }
        gameObject.layer = originalLayer;
    }  
}
