using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class projectile : MonoBehaviour
{
    [SerializeField] private float speed = 10.0f;
    private int damage;

    private Rigidbody2D rb;
    private Character character;
    private Character owner;

    public void Initialize(Character target,Character owner,int dmg)
    {
        character = target;
        this.owner = owner;
        damage = dmg;
    }
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    private void Start()
    {
        Destroy(gameObject, 5.0f);
    }
    private void FixedUpdate()
    {
        if(owner == null || !owner.gameObject.activeSelf)
        {
            Destroy(gameObject);
            return;
        }

        if(character == null || !character.gameObject.activeSelf)
        {
            Destroy(gameObject);
            return;
        }
        

        Vector2 dir = ((Vector2)character.transform.position-rb.position).normalized;
        float angle = Mathf.Atan2(dir.y,dir.x)*Mathf.Rad2Deg;
        rb.MoveRotation(angle);

        rb.MovePosition(rb.position + dir * speed * Time.fixedDeltaTime);

        if(Vector2.Distance(rb.position,character.transform.position)<0.3f)
        {
            character.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
    
}
