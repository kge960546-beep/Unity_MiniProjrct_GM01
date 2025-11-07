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
    private float lastAttackTime;   
    public bool isDead = false;
    private bool skillReady;
    private bool isAttacking = false;

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
    public bool isBattle = false;

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
        Time.timeScale = 0;
        Upgrade();
        ChangeState(State.Idle);

        TextManager tm = FindObjectOfType<TextManager>();
        if (tm != null)
        {
            bool isFromBattleField = (dragController == null || !dragController.isSpawnZone);

            if (isFromBattleField)
            {
                if (isEnemy)
                    tm.enemyUnit.Add(gameObject);
                else
                    tm.friendlyUnit.Add(gameObject);
            }
        }
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
        if(isBattle)
        {
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
    }
    public void ChangeState(State newState)
    {        
        if (presentState == newState) return;
        State oldState = presentState;
        presentState = newState;

        if(newState == State.Moving && oldState == State.Idle)
        {
            if (presentTarget != null)
            {
                lastPathUpdate = Time.time;
                Character targetChar = presentTarget.GetComponent<Character>();
                if(targetChar != null)
                {
                    finalAttackPos = FindAttackPosition(targetChar);
                    RePath();
                }
                else
                {
                    presentTarget = null;
                    ChangeState(State.Idle);
                }                
            }
            else
            {
                ChangeState(State.Idle);
            }
        }
        if (anim != null)
        {
            anim.SetInteger("State", (int)newState);
            if (newState == State.Attacking)
            {
                anim.SetFloat("attackSpeed", currentAttackSpeed);
                //lastAttackTime = Time.time - (1.0f / currentAttackSpeed);
            }
        }    
    }
    //========================대기 상태=======================
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
    //========================이동 상태=========================
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
        if (GameManager.Instance.map == null) return;

            Vector2Int start = new Vector2Int(Mathf.FloorToInt(transform.position.x) + GameManager.Instance.mapOffset.x, Mathf.FloorToInt(transform.position.y) + GameManager.Instance.mapOffset.y);
        Vector2Int end = new Vector2Int(Mathf.FloorToInt(finalAttackPos.x) + GameManager.Instance.mapOffset.x, Mathf.FloorToInt(finalAttackPos.y) + GameManager.Instance.mapOffset.y);

        start.x = Mathf.Clamp(start.x, 0, GameManager.Instance.width - 1);
        start.y = Mathf.Clamp(start.y, 0, GameManager.Instance.height - 1);
        end.x = Mathf.Clamp(end.x, 0, GameManager.Instance.width - 1);
        end.y = Mathf.Clamp(end.y, 0, GameManager.Instance.height - 1);

        if (!GameManager.Instance.map[end.x, end.y])
        {
            Debug.LogWarning($"{name}의 목표 지점({end.x}, {end.y})이 이동 불가 지역입니다. 주변 타일을 탐색합니다.");

            Vector2Int[] offsets = { new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(1, 0),
                                 new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1) }; // 대각선 포함

            float closestDist = float.MaxValue;
            Vector2Int newEnd = end;
            bool foundNewEnd = false;

            foreach (var offset in offsets)
            {
                Vector2Int checkPos = end + offset;
               
                if (checkPos.x < 0 || checkPos.x >= GameManager.Instance.width || checkPos.y < 0 || checkPos.y >= GameManager.Instance.height)
                    continue;
               
                if (GameManager.Instance.map[checkPos.x, checkPos.y])
                {                   
                    float dist = Vector2Int.Distance(start, checkPos);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        newEnd = checkPos;
                        foundNewEnd = true;
                    }
                }
            }
           
            if (foundNewEnd)
            {               
                end = newEnd;
            }
        }

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
    private Vector3 FindAttackPosition(Character target)
    {       
        if (GameManager.Instance.map == null)
        {           
            return transform.position;
        }

        Vector2Int myGridPos = new Vector2Int(
            Mathf.FloorToInt(transform.position.x) + GameManager.Instance.mapOffset.x,
            Mathf.FloorToInt(transform.position.y) + GameManager.Instance.mapOffset.y
        );

        Vector2Int targetGridPos = new Vector2Int(
            Mathf.FloorToInt(target.transform.position.x) + GameManager.Instance.mapOffset.x,
            Mathf.FloorToInt(target.transform.position.y) + GameManager.Instance.mapOffset.y
        );
       
        Vector2Int[] offsets = { new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(1, 0) };

        float closestDist = float.MaxValue;
        Vector2Int bestSpot = targetGridPos;
        bool foundValidSpot = false;

        foreach (var offset in offsets)
        {
            Vector2Int checkPos = targetGridPos + offset;
           
            if (checkPos.x < 0 || checkPos.x >= GameManager.Instance.width || checkPos.y < 0 || checkPos.y >= GameManager.Instance.height)
            {
                continue;
            }
          
            if (GameManager.Instance.map[checkPos.x, checkPos.y])
            {                
                float dist = Vector2Int.Distance(myGridPos, checkPos);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    bestSpot = checkPos;
                    foundValidSpot = true;
                }
            }
        }
        
        if (!foundValidSpot)
        {           
            bestSpot = targetGridPos;
        }
        
        return new Vector3(
            bestSpot.x - GameManager.Instance.mapOffset.x + 0.5f,
            bestSpot.y - GameManager.Instance.mapOffset.y + 0.5f,
            0
        );
    }
    //=========================공격 상태===========================
    public void AttackTarget()
    {       
        if (presentTarget == null || !presentTarget.gameObject.activeSelf)
        {
            presentTarget = null;          
            ChangeState(State.Idle);
            return;
        }
       
        float distance = Vector2.Distance(transform.position, presentTarget.position);

        if (dragController != null && dragController.isSpawnZone)
        {
            ChangeState(State.Idle);
            return;
        }
        
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
     
        if(!isAttacking && (Time.time - lastAttackTime) >= 1.0f / currentAttackSpeed || skillReady)
        {
            Debug.Log($"[{gameObject.name}] AttackTarget() 진입됨. skillReady = {skillReady}");
            isAttacking = true;

            if (skillReady)
            {
                Debug.Log($"[{gameObject.name}] useSkill Trigger 발동됨!");
                anim.SetTrigger("useSkill");
                skillReady = false;
            }
            else
            {
                Debug.Log($"[{gameObject.name}] 일반 Attack Trigger 발동됨!");
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
        HpSidle hpBar = GetComponentInChildren<HpSidle>();
        if(hpBar != null)
        {
            hpBar.onHpChange?.Invoke(presentHP, data.HPMax);
        }
        if (presentHP <= 0) Die();
    }    
    public void TakeSkillDamage(int skillDamage)
    {
        Debug.Log(gameObject.name + "이(가) 스킬 데미지 " + skillDamage + "를 받았습니다!");
        if (dragController != null && dragController.isSpawnZone)
        {
            ChangeState(State.Idle);
            return;
        }        
        presentHP -= skillDamage;
        StartCoroutine(hitColor());
        HpSidle hpBar = GetComponentInChildren<HpSidle>();
        if (hpBar != null)
        {
            hpBar.onHpChange?.Invoke(presentHP, data.HPMax);
        }
        if (presentHP <= 0) Die();
    }
    public void ManaGain()
    {
        gainMP = data.MPRefill;
        presentMP = Mathf.Min(presentMP + gainMP, data.MPMax);
        MpSidle mpBar = GetComponentInChildren<MpSidle>();
        if (mpBar != null)
        {
            mpBar.RegenerateMana(gainMP);
        }
        if (presentMP >= data.MPMax)
        {            
            skillReady = true;
            presentMP = data.MPMax;
        }
    }
    private int hitStack = 0;
    IEnumerator hitColor()
    {
        if(spriteRenderer == null) yield break;
        hitStack++;
        Color originColor = spriteRenderer.color;

        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.3f);
        spriteRenderer.color = originColor;
        hitStack--;
        if(hitStack <= 0)
        {
            hitStack = 0;
            spriteRenderer.color = originColor;
        }
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

        TextManager tm = FindObjectOfType<TextManager>();
        if (tm != null)
        {
            if (isEnemy)
                tm.enemyUnit.Remove(gameObject);
            else
                tm.friendlyUnit.Remove(gameObject);
        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.saveUnits.RemoveAll(u =>
                u.unitName == gameObject.name &&
                Vector3.Distance(u.position, transform.position) < 0.2f
            );
        }

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
        skillReady = false;
        presentMP = 0;
        MpSidle mpBar = GetComponentInChildren<MpSidle>();
        if (mpBar != null)
        {
            mpBar.UseMana(data.MPMax); // 전부 소모
        }
    }
    void BlowHit()
    {
        Debug.Log($"[BlowSkill] target: {presentTarget}, active: {(presentTarget ? presentTarget.gameObject.activeSelf : false)}");
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
        Debug.Log($"[{gameObject.name}] BlowSkill() 실행됨");
        if (presentTarget == null) return;
        Character enemy = presentTarget.GetComponent<Character>();
        if (enemy == null || !enemy.gameObject.activeSelf) return;

        enemy.TakeSkillDamage(currentSkillPower);
        skillReady = false;
        presentMP = 0;
        MpSidle mpBar = GetComponentInChildren<MpSidle>();
        if (mpBar != null)
        {
            mpBar.UseMana(data.MPMax); // 전부 소모
        }
    }
    public void FinishAttack()
    {
        isAttacking = false;
    }
    void OnDisable()
    {
        TextManager tm = FindObjectOfType<TextManager>();
        if (tm != null)
        {
            if (isEnemy)
                tm.enemyUnit.Remove(gameObject);
            else
                tm.friendlyUnit.Remove(gameObject);
        }
    }    
}