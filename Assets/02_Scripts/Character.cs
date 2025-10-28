using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class Character : MonoBehaviour
{

    public CharacterData data;
    private SpriteRenderer spriteRenderer;
    private Animator anim;
    public bool isEnemy;

    private int presentHP;
    private float lastAttackTime;
    private bool isHit = false;
    private bool isDead = false;

    private Transform presentTarget;

    public enum State {Idle,Moving,Attacking}
    private State presentState = State.Idle;    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
    }
    void Start()
    {
        presentHP = data.HPMax;
        ChangeState(State.Idle);
    }
    void Update()
    {        
        switch(presentState)
        {
            case State.Idle:
                SearchTarget(); break;
            case State.Moving:
                MoveTarget(); break;
            case State.Attacking:
                AttackTarget(); break;
        }
    }
    void ChangeState(State newState)
    {
        Debug.Log("ChangeState → " + newState);
        if (presentState == newState) return;
        presentState = newState;
        if (anim != null)
        {
            anim.SetInteger("State", (int)newState);
            if (newState == State.Attacking)
            {
                anim.SetFloat("attackSpeed", data.AttackSpeed);
                lastAttackTime = Time.time - (1.0f / data.AttackSpeed);
            }
        }    
    }   
    public void SearchTarget()
    {
        Character nearestTarget = null;
        float nearestDistance = Mathf.Infinity;
        foreach (Character other in FindObjectsOfType<Character>())
        {
            if (other == this) continue;
            if(other.isEnemy == isEnemy) continue;
            if (!other.gameObject.activeSelf) continue;
            float dist = Vector2.Distance(transform.position, other.transform.position);
            if (dist<nearestDistance)
            {
               nearestDistance = dist;
                nearestTarget = other;
            }
        }

        if(nearestTarget != null)
        {
            presentTarget = nearestTarget.transform;
            ChangeState(State.Moving);           
        }
    }
    public void MoveTarget()
    {
        if(presentTarget == null)
        {
            ChangeState(State.Idle);
           
            return;
        }
        float distance = Vector2.Distance(transform.position, presentTarget.position);
        if(distance <= data.attackRange)
        {

            ChangeState(State.Attacking);
            
            return;
        }
        if(presentTarget.position.x<transform.position.x)
        {
            spriteRenderer.flipX = true;
        }
        else { spriteRenderer.flipX = false;}
            transform.position = Vector2.MoveTowards(transform.position, presentTarget.position, data.moveSpeed * Time.deltaTime);        
    }
    public void AttackTarget()
    {
        float atkSpeed = data.AttackSpeed;
        int atkPower = data.AttackPower;
        float distance = Vector2.Distance(transform.position, presentTarget.position);

        if (presentTarget == null) { SearchTarget(); return; }

        if(distance > data.attackRange)
        {
            ChangeState(State.Moving);
          
            MoveTarget();
            return;
        }

        transform.position = transform.position;

        if(Time.time - lastAttackTime >= 1.0f / atkSpeed)
        {
            if(data.isRenged)
            {
                anim.SetTrigger("Attack");
                lastAttackTime = Time.time;
            }
            else
            {                
                Character enemy = presentTarget.GetComponent<Character>();
                if (enemy == null) { ChangeState(State.Idle); return; }
                anim.SetTrigger("Attack");                
                lastAttackTime = Time.time;
            }
                     
        }
        if(!presentTarget.gameObject.activeSelf)
        {
            presentTarget = null;
            ChangeState(State.Idle);           
            return;
        }
    }
    public void TakeDamage(int damage)
    {
        if (isHit) return;

        presentHP -= damage;        

        StartCoroutine(hitColor());

        if (presentHP <= 0) Die();
    }
    IEnumerator hitColor()
    {        
        isHit = true;
        if(spriteRenderer == null) yield break;
        Color originColor = spriteRenderer.color;

        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originColor;
        isHit = false;        
    }
    void Die()
    {
        if(isDead) return;
        isDead = true;

        if(anim != null)
        {            
            anim.SetTrigger("Dead");
        }
        StartCoroutine(Dead());
    }
    IEnumerator Dead()
    {
        yield return new WaitForSeconds(0.4f);
        gameObject.SetActive(false);
    }   

    private projectile projectilleCs;
    void FirePeojectile()
    {        
        if (data.projectilePrefab == null || presentTarget == null) return;
        //복제
        GameObject projectileObj = Instantiate(data.projectilePrefab, transform.position, Quaternion.identity);
        //스크립트 가져오기
        projectilleCs = projectileObj.GetComponent<projectile>();
        //공격력과 타겟 설정
        projectilleCs.Initialize(presentTarget.GetComponent<Character>(),this,data.AttackPower);
    }
    void BlowHit()
    {
        if (presentTarget == null) return;

        float atkSpeed = data.AttackSpeed;
        int atkPower = data.AttackPower;
        float distance = Vector2.Distance(transform.position, presentTarget.position);
        Character enemy = presentTarget.GetComponent<Character>();
        if (enemy == null || !enemy.gameObject.activeSelf) return;
        enemy.TakeDamage(atkPower);
    }
}
