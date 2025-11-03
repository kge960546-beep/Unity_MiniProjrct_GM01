using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Character : MonoBehaviour
{
    [Header("능력치")]
    public int presentHP;
    public int presentMP;
    public int currentAttackPower;
    public float currentAttackSpeed;
    public int currentSkillPower;
    public int gainMP;

    public CharacterData data;
    private SpriteRenderer spriteRenderer;
    private Animator anim;

    public bool isEnemy;
    private bool isUsingSkill = false;
    private float lastAttackTime;   
    private bool isDead = false;

    private Transform presentTarget;
    public DragController dragController;

    [Header("A*알고리즘")]
    public LayerMask obstacleLayer;
    private List<Vector2Int> path;
    private int pathIndex = 0;
    private float pathUpdate = 1.5f;
    private float pathUpdateCollTime = 0.5f;
    private float lastPathUpdate;
    private Vector2 finalAttackPos;
    public enum State {Idle,Moving,Attacking}
    private State presentState = State.Idle;

    public int star = 1;

    [Header("AI 전투 오프셋")]
    [SerializeField] private Vector2 attackOffset;

    
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        dragController = GetComponent<DragController>();
        attackOffset = new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f));
    }
    void Start()
    {
        Upgrade();
        ChangeState(State.Idle);
    }    
    void Update()
    {
        if (!isEnemy && star > 1) // 내 유닛이고 1성보다 높을 때만 로그 출력
        {
            Debug.Log(gameObject.name + " Update - HP: " + presentHP + ", ATK: " + currentAttackPower);
        }

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
        State oldState = presentState;
        presentState = newState;

        if(newState == State.Moving && oldState == State.Idle)
        {
            if (presentTarget != null)
            {
                lastPathUpdate = Time.time;
                finalAttackPos = (Vector2)presentTarget.position+attackOffset;
                RePath();
            }
            
        }
        if (anim != null)
        {
            anim.SetInteger("State", (int)newState);
            if (newState == State.Attacking)
            {
                anim.SetFloat("attackSpeed", currentAttackSpeed);
                lastAttackTime = Time.time - (1.0f / currentAttackSpeed);
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
        if (presentTarget == null || !presentTarget.gameObject.activeSelf)
        {
            presentTarget = null;
            path = null;
            ChangeState(State.Idle);
            return;
        }

        float DistTarget = Vector2.Distance(transform.position, presentTarget.position);

        if (DistTarget <= data.attackRange)
        {
            path = null;
            ChangeState(State.Attacking);
            return;
        }

        bool isTimeForUpdate = Time.time - lastPathUpdate > pathUpdateCollTime;
        bool isTargetTooFar = Vector2.Distance(finalAttackPos, (Vector2)presentTarget.position + attackOffset) > pathUpdate;
        if (isTimeForUpdate && isTargetTooFar)
        {
            // 쿨타임 타이머와 목적지를 현재 시간/위치로 갱신
            lastPathUpdate = Time.time;
            finalAttackPos = (Vector2)presentTarget.position + attackOffset;
            RePath();
        }
        if (path != null && path.Count > 0)
        {
            Vector2 targetPos = new Vector2(path[pathIndex].x - GameManager.Instance.mapOffset.x + 0.5f,
                                        path[pathIndex].y - GameManager.Instance.mapOffset.y + 0.5f);
            transform.position = Vector2.MoveTowards(transform.position, targetPos, data.moveSpeed * Time.deltaTime);

            if (Vector2.Distance(transform.position, targetPos) < 0.1f)
            {
                pathIndex++;

                if (pathIndex >= path.Count - 1)
                {
                    path.Clear();
                }
            }
        }
        else
        {
            RaycastHit2D hit = Physics2D.Linecast(transform.position, presentTarget.position, obstacleLayer);

            if (hit.collider == null)
            {
                path = null;
                transform.position = Vector2.MoveTowards(transform.position, (Vector2)presentTarget.position + attackOffset, data.moveSpeed * Time.deltaTime);
            }
            else
            {            
                if (path == null || (isTimeForUpdate && isTargetTooFar))
                {
                    lastPathUpdate = Time.time;
                    finalAttackPos = (Vector2)presentTarget.position + attackOffset;
                    RePath();
                }
              
                if (path != null && pathIndex < path.Count)
                {
                    Vector2 targetPos = new Vector2(path[pathIndex].x - GameManager.Instance.mapOffset.x + 0.5f,
                                                    path[pathIndex].y - GameManager.Instance.mapOffset.y + 0.5f);
                    transform.position = Vector2.MoveTowards(transform.position, targetPos, data.moveSpeed * Time.deltaTime);

                    if (Vector2.Distance(transform.position, targetPos) < 0.1f)
                    {
                        pathIndex++;
                        if (pathIndex >= path.Count)
                        {
                            path = null;
                        }
                    }
                }
            }
        }

        if (spriteRenderer != null)
            spriteRenderer.flipX = (presentTarget.position.x < transform.position.x);

        transform.position = new Vector3(Mathf.Clamp(transform.position.x, GameManager.Instance.battleMapMin.x, GameManager.Instance.battleMapMax.x),
                                         Mathf.Clamp(transform.position.y, GameManager.Instance.battleMapMin.y, GameManager.Instance.battleMapMax.y),
                                         transform.position.z);
       
    }
    public void RePath()
    {
        if (presentTarget == null) return;

        Vector2Int start = new Vector2Int(Mathf.FloorToInt(transform.position.x) + GameManager.Instance.mapOffset.x, Mathf.FloorToInt(transform.position.y) + GameManager.Instance.mapOffset.y);
        Vector2Int end = new Vector2Int(Mathf.FloorToInt(finalAttackPos.x) + GameManager.Instance.mapOffset.x, Mathf.FloorToInt(finalAttackPos.y) + GameManager.Instance.mapOffset.y);

        start.x = Mathf.Clamp(start.x, 0, GameManager.Instance.width - 1);
        start.y = Mathf.Clamp(start.y, 0, GameManager.Instance.height - 1);
        end.x = Mathf.Clamp(end.x, 0, GameManager.Instance.width - 1);
        end.y = Mathf.Clamp(end.y, 0, GameManager.Instance.height - 1);

        path = Node.FindPath(GameManager.Instance.map, start, end);
        if (path != null && path.Count > 1)
        {
            if (path[0] == start)
            {
                path.RemoveAt(0);
            }
        }
        pathIndex = 0;
    }
    public void AttackTarget()
    {
        if (presentTarget == null || !presentTarget.gameObject.activeSelf)
        {            
            path = null;
            ChangeState(State.Idle);
            return;
        }
       
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
            ChangeState(State.Idle);
            return;
        }

        if(distance > data.attackRange)
        {
            ChangeState(State.Moving);            
            return;
        }
        else
        {
            transform.position = transform.position;            
        }
        if (isUsingSkill) return;
        if(Time.time - lastAttackTime >= 1.0f / currentAttackSpeed)
        {
            bool isSkill = (presentMP >= data.MPMax);
            if (isSkill)
            {
                isUsingSkill = true;
                anim.SetTrigger("useSkill");                
            }
            else
            {                
                if (enemy == null) { ChangeState(State.Idle); return; }
                anim.SetTrigger("Attack");                
            }
            lastAttackTime = Time.time;
        }
       
        if (!presentTarget.gameObject.activeSelf)
        {
            presentTarget = null;
            ChangeState(State.Idle);           
            return;
        }

        if (spriteRenderer != null)
            spriteRenderer.flipX = (presentTarget.position.x < transform.position.x);
    }
    public void TakeDamage(int damage)
    {
        if (dragController != null && dragController.isSpawnZone)
        {
            ChangeState(State.Idle);
            return;
        }       

        presentHP -= damage;
        StartCoroutine(hitColor());
        if (presentHP <= 0) Die();
    }    
    public void TakeSkillDamage(int skillDamage)
    {
        if (dragController != null && dragController.isSpawnZone)
        {
            ChangeState(State.Idle);
            return;
        }        
        presentHP -= skillDamage;
        StartCoroutine(hitColor());
        if (presentHP <= 0) Die();
    }
    public void ManaGain()
    {
        gainMP = data.MPRefill;
        presentMP += gainMP;
        presentMP = Mathf.Clamp(presentMP, 0, data.MPMax);       
    }
   
    IEnumerator hitColor()
    {                
        if(spriteRenderer == null) yield break;
        Color originColor = spriteRenderer.color;

        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originColor;               
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
        //isHit = false;
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
    public void Upgrade() 
    {
        float hpUp = Mathf.Pow(1.35f, star - 1); 
        float atkUp = Mathf.Pow(1.35f, star - 1); 
        float atsUp = Mathf.Pow(1.35f, star - 1);
        float skillUp = Mathf.Pow(1.35f,star -1);
        presentHP = Mathf.RoundToInt(data.HPMax * hpUp);
        currentAttackPower = Mathf.RoundToInt(data.AttackPower * atkUp);
        currentAttackSpeed = data.AttackSpeed * atkUp;
        currentSkillPower = Mathf.RoundToInt(data.skillDamage * skillUp);
    }
    private projectile projectilleCs;
    void FirePeojectile()
    {
        if (dragController != null && dragController.isSpawnZone)
            return;
        if (presentTarget == null || !presentTarget.gameObject.activeSelf) return;
        if (data.projectilePrefab == null) return;
        //복제
        GameObject projectileObj = Instantiate(data.projectilePrefab, transform.position, Quaternion.identity);
        //스크립트 가져오기
        projectilleCs = projectileObj.GetComponent<projectile>();
        //공격력과 타겟설정
        projectilleCs.Initialize(presentTarget.GetComponent<Character>(), this, currentAttackPower, false);
              
        ManaGain();
    }   
    void FireSkill()
    {       
        switch (data.SkillClassification)
        {
            case SkillClassification.skillEffectOn:
                // 즉발형: 타겟 위치에 이펙트만 재생 + 즉시 데미지, projectile 사용 X
                if (data.skillEffectPrefab == null) return;
                Instantiate(data.skillEffectPrefab, presentTarget.position, Quaternion.identity);

                Character enemy = presentTarget.GetComponent<Character>();
                if (enemy != null) enemy.TakeSkillDamage(currentSkillPower);                             
                break;

            case SkillClassification.justPrefab:
                // 발사형: 캐스터 위치에서 스킬용 발사체 발사
                if (data.skillprojectilePrefab == null) return;

                GameObject projectileSkill = Instantiate(data.skillprojectilePrefab, transform.position, Quaternion.identity);
                projectilleCs = projectileSkill.GetComponent<projectile>();
                projectilleCs.Initialize(presentTarget.GetComponent<Character>(), this, currentSkillPower, true);                           
                break;
        }
        presentMP = 0;
    }
    void BlowHit()
    {
        if (dragController != null && dragController.isSpawnZone)
            return;
        if (presentTarget == null) return;
        
        Character enemy = presentTarget.GetComponent<Character>();
        if (enemy == null || !enemy.gameObject.activeSelf) return;
        enemy.TakeDamage(currentAttackPower);
              
        ManaGain();
    }
    void BlowSkill()
    {
        if (presentTarget == null) return;
        Character enemy = presentTarget.GetComponent<Character>();
        if (enemy == null || !enemy.gameObject.activeSelf) return;

        enemy.TakeSkillDamage(currentSkillPower);
        presentMP = 0;
    }
    void SkillEnd()
    {
        //anim.SetInteger("State", 0);
        isUsingSkill = false;
    }
}