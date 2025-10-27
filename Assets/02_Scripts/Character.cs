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

    private int currentHP;
    private float lastAttackTime;
    private bool isHit = false;

    private Transform currentTarget;

    public enum State {Idle,Moving,Attacking}
    private State currentState = State.Idle;    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
    }
    void Start()
    {
        currentHP = data.HPMax;
        ChangeState(State.Idle);
    }
    void Update()
    {
        if (isHit) return;

        switch(currentState)
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
        if (currentState == newState) return;
        currentState = newState;
        if (anim != null)
        {
            anim.SetInteger("State",(int)newState);
            if(newState == State.Attacking)
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
            currentTarget = nearestTarget.transform;
            ChangeState(State.Moving);
           
        }
    }
    public void MoveTarget()
    {
        if(currentTarget == null)
        {
            ChangeState(State.Idle);
           
            return;
        }
        float distance = Vector2.Distance(transform.position, currentTarget.position);
        if(distance <= data.attackRange)
        {
            ChangeState(State.Attacking);
           
            return;
        }        
        transform.position = Vector2.MoveTowards(transform.position, currentTarget.position, data.moveSpeed * Time.deltaTime);        
    }
    public void AttackTarget()
    {
        float atkSpeed = data.AttackSpeed;
        int atkPower = data.AttackPower;
        float distance = Vector2.Distance(transform.position, currentTarget.position);

        if (currentTarget == null) { SearchTarget(); return; }

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
                Character enemy = currentTarget.GetComponent<Character>();
                if (enemy == null) { ChangeState(State.Idle); return; }
                enemy.TakeDamage(atkPower);
                lastAttackTime = Time.time;
            }
                     
        }
        if(!currentTarget.gameObject.activeSelf)
        {
            currentTarget = null;
            ChangeState(State.Idle);
           
            return;
        }
    }
    public void TakeDamage(int damage)
    {
        if (isHit) return;

        currentHP -= damage;        

        StartCoroutine(hitColor());

        if (currentHP <= 0) Die();

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
       if(anim != null)
        {
            anim.SetTrigger("Dead");
        }

       StartCoroutine(Dead());
    }
    IEnumerator Dead()
    {
        yield return new WaitForSeconds(0.5f);
        gameObject.SetActive(false);
    }   

    private projectile projectilleCs;
    void FirePeojectile()
    {        
        if (data.projectilePrefab == null || currentTarget == null) return;
        //복제
        GameObject projectileObj = Instantiate(data.projectilePrefab, transform.position, Quaternion.identity);
        //스크립트 가져오기
        projectilleCs = projectileObj.GetComponent<projectile>();
        //공격력과 타겟 설정
        projectilleCs.Initialize(currentTarget.GetComponent<Character>(),this,data.AttackPower);
    }
}
