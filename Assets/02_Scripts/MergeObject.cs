using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MergeObject : MonoBehaviour
{
    public int star;
    public int id;
    public GameObject nextStarPrefab;

    private Vector2 mousePos;
    private float offsetX, offsetY;
    public static bool mouseButtonReleased;
    private void OnMouseDown()
    {
        mouseButtonReleased = false;
        offsetX = Camera.main.ScreenToWorldPoint( Input.mousePosition ).x-transform.position.x;
        offsetY = Camera.main.ScreenToWorldPoint( Input.mousePosition ).y-transform.position.y;
    }
    private void OnMouseDrag()
    {
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        transform.position = new Vector2(mousePos.x - offsetX, mousePos.y - offsetY);
    }
    private void OnMouseUp()
    {
        mouseButtonReleased = true;
    }
    private void OnTriggerStay2D(Collider2D collision)
    {
        MergeObject otherUnit = collision.GetComponent<MergeObject>();
        if (otherUnit == null) return;

        if (mouseButtonReleased && this.id == otherUnit.id && this.star == otherUnit.star)
        {
            string nextPrefab = id + "_" + (star + 1);
            GameObject nextLevelUnitPrefab = Resources.Load<GameObject>(nextPrefab);
            if(nextLevelUnitPrefab != null)
            {
                Instantiate(Resources.Load("All_1"), transform.position, Quaternion.identity);                
                Destroy(collision.gameObject);
                Destroy(gameObject);
            }
            else
            {
                Debug.LogError(nextPrefab + " 프리팹을 Resources 폴더에서 찾을 수 없습니다!");
            }
            mouseButtonReleased = false;

        }       
    }
}
