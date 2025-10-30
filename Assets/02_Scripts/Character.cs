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
    public DragController dragController;

    private List<Vector2Int> path;
    private int pathIndex = 0;
    private float repathTimer = 0.0f;

    public enum State {Idle,Moving,Attacking}
    private State presentState = State.Idle;    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        dragController = GetComponent<DragController>();
    }
    void Start()
    {
        presentHP = data.HPMax;
        ChangeState(State.Idle);
    }
    void Update()
    {
        Debug.Log($"{name} state = {presentState}, isSpawnZone={dragController?.isSpawnZone}");
        if (dragController == null)
            Debug.LogWarning($"{name}: dragController is NULL");
        if (data == null)
            Debug.LogWarning($"{name}: data is NULL");
        if (anim == null)
            Debug.LogWarning($"{name}: anim is NULL");
        if (spriteRenderer == null)
            Debug.LogWarning($"{name}: spriteRenderer is NULL");

        if (dragController != null)
        {
            if(dragController.isSpawnZone)
            {
                presentTarget = null;
                ChangeState(State.Idle);
                if (anim != null)
                {
                    anim.ResetTrigger("Attack");
                    anim.SetInteger("State", (int)State.Idle);
                }
                return;
            }            
        }
        
        switch (presentState)
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
        if (dragController != null && dragController.isSpawnZone)
            return;

        Character nearestTarget = null;
        float nearestDistance = Mathf.Infinity;

        foreach (Character other in FindObjectsOfType<Character>())
        {
            if (other == this) continue;
            if(other.isEnemy == isEnemy) continue;
            if (!other.gameObject.activeSelf) continue;
            if (other.dragController != null && other.dragController.isSpawnZone) continue;

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
        repathTimer += Time.deltaTime;
        if(path == null || repathTimer > 0.5f)
        {
            repathTimer = 0.0f;
            Vector2Int start = new Vector2Int(Mathf.RoundToInt(transform.position.x),Mathf.RoundToInt(transform.position.y));
            Vector2Int end = new Vector2Int(Mathf.RoundToInt(presentTarget.position.x),Mathf.RoundToInt(presentTarget.position.y));

            if (GameManager.Instance.map != null)
                path = Node.FindPath(GameManager.Instance.map, start, end);

            pathIndex = 0;
        }
        if(path == null || path.Count == 0)
        {
            ChangeState(State.Idle);
            return;
        }

        Vector2 targetPos = new Vector2(path[pathIndex].x + 0.5f, path[pathIndex].y + 0.5f);

        transform.position = Vector2.MoveTowards(transform.position, targetPos, data.moveSpeed * Time.deltaTime);

        if(Vector2.Distance(transform.position,targetPos)<0.1f)
        {
            pathIndex++;
        }

        if(pathIndex >= path.Count -1)
        {
            float dist = Vector2.Distance(transform.position, presentTarget.position);
            if (dist <= data.attackRange)
                ChangeState(State.Attacking);
        }

        spriteRenderer.flipX = (presentTarget.position.x < transform.position.x);      
    }
    public void AttackTarget()
    {
        float atkSpeed = data.AttackSpeed;
        int atkPower = data.AttackPower;
        float distance = Vector2.Distance(transform.position, presentTarget.position);

        if (dragController != null && dragController.isSpawnZone)
        {
            ChangeState(State.Idle);
            return;
        }

        if (presentTarget == null) { SearchTarget(); return; }
        Character enemy = presentTarget.GetComponent<Character>();
        if(enemy != null && enemy.dragController != null && enemy.dragController.isSpawnZone)
        {
            presentTarget = null;
            SearchTarget();
            return;
        }

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
                if (enemy == null) { ChangeState(State.Idle); return; }
                anim.SetTrigger("Attack");                
                lastAttackTime = Time.time;
            }
                     
        }
       
        if (!presentTarget.gameObject.activeSelf)
        {
            presentTarget = null;
            ChangeState(State.Idle);           
            return;
        }
    }
    public void TakeDamage(int damage)
    {
        if (dragController != null && dragController.isSpawnZone)
        {
            ChangeState(State.Idle);
            return;
        }

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

    public void ReSetState()
    {
        presentTarget = null;
        ChangeState(State.Idle);
        isHit = false;
        isDead = false;

        if(anim != null)
        {
            anim.ResetTrigger("Attack");
            anim.SetInteger("State", (int)State.Idle);
        }
    }
    private void OnDrawGizmos()
    {
        if (path == null) return;
        Gizmos.color = Color.green;
        for(int i = 0; i < path.Count; i++)
        {
            Gizmos.DrawSphere(new Vector3(path[i].x + 0.5f, path[i].y + 0.5f, 0), 0.1f);
        }
    }
    private projectile projectilleCs;
    void FirePeojectile()
    {
        if (dragController != null && dragController.isSpawnZone)
            return;
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
        if (dragController != null && dragController.isSpawnZone)
            return;
        if (presentTarget == null) return;

        float atkSpeed = data.AttackSpeed;
        int atkPower = data.AttackPower;
        float distance = Vector2.Distance(transform.position, presentTarget.position);
        Character enemy = presentTarget.GetComponent<Character>();
        if (enemy == null || !enemy.gameObject.activeSelf) return;
        enemy.TakeDamage(atkPower);
    }
}
