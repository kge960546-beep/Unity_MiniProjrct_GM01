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
    private Vector3 lastTargetPos;
    private float smootDistEnemy = 0.0f;

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
        if (dragController != null && dragController.isSpawnZone)
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

        
        float DistTarget = Vector2.Distance(transform.position, presentTarget.position);
        
        if (path == null || repathTimer > 2.5f)
        {
            float moveDist = Vector2.Distance(presentTarget.position, lastTargetPos);
            if(moveDist > 1.0f || path == null)
            {
                repathTimer = 0.0f;
                lastTargetPos = presentTarget.position;
                pathIndex = 0;
               
                Vector2Int start = new Vector2Int(Mathf.FloorToInt(transform.position.x) + GameManager.Instance.mapOffset.x,
                                                  Mathf.FloorToInt(transform.position.y) + GameManager.Instance.mapOffset.y);
               
                Vector2Int end = new Vector2Int(Mathf.FloorToInt(presentTarget.position.x) + GameManager.Instance.mapOffset.x,
                                                Mathf.FloorToInt(presentTarget.position.y) + GameManager.Instance.mapOffset.y);
               
                start.x = Mathf.Clamp(start.x, 0, GameManager.Instance.width - 1);
                start.y = Mathf.Clamp(start.y, 0, GameManager.Instance.height - 1);
                end.x = Mathf.Clamp(end.x, 0, GameManager.Instance.width - 1);
                end.y = Mathf.Clamp(end.y, 0, GameManager.Instance.height - 1);
               
                Debug.Log($"{name} : start={start}, end={end} map size={GameManager.Instance.width}x{GameManager.Instance.height}");
               
                path = Node.FindPath(GameManager.Instance.map, start, end);
                Debug.Log($"{name}: path 재계산됨, pathIndex={pathIndex}, pathCount={(path == null ? 0 : path.Count)}");
            }
            repathTimer += Time.deltaTime;
        }
        if (path == null)
        {
            Debug.LogWarning($"{name}: path == null (경로 없음)");
            pathIndex = 0;
            path = null;
            ChangeState(State.Idle);
            SearchTarget();
            return;
        }
        if (path.Count == 0)
        {
            Debug.LogWarning($"{name}: path.Count == 0 (빈 경로)");
            ChangeState(State.Idle);
            return;
        }
        
        Vector2 targetPos = new Vector2(path[pathIndex].x - GameManager.Instance.mapOffset.x,
                                        path[pathIndex].y - GameManager.Instance.mapOffset.y);
        Debug.DrawLine(transform.position, targetPos, Color.black);
        
        transform.position = Vector2.MoveTowards(transform.position, targetPos, data.moveSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, targetPos) < 0.2f)
        {
            if (pathIndex < path.Count - 1)
            {
                pathIndex++;
            }
        }
        else
        {
            if (DistTarget <= data.attackRange)
            {
                ChangeState(State.Attacking);
                return;
            }
            else
            {
                transform.position = Vector2.MoveTowards(
               transform.position,
               presentTarget.position,
               (data.moveSpeed * 0.5f) * Time.deltaTime);
            }
        }

        if (spriteRenderer != null)
            spriteRenderer.flipX = (presentTarget.position.x < transform.position.x);

        transform.position = new Vector3(Mathf.Clamp(transform.position.x, GameManager.Instance.battleMapMin.x, GameManager.Instance.battleMapMax.x),
                                         Mathf.Clamp(transform.position.y, GameManager.Instance.battleMapMin.y, GameManager.Instance.battleMapMax.y),
                                         transform.position.z);
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
        else
        {
            transform.position = transform.position;            
        }

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
            Gizmos.DrawSphere(new Vector3(path[i].x - GameManager.Instance.mapOffset.x + 0.5f,
                                          path[i].y - GameManager.Instance.mapOffset.y + 0.5f, 0), 0.1f);
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
