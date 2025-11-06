/*
 Character 스크립트 복원용 (중간에 막혀서 다가가지않는 현상있는스크립트)

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
    private float pathUpdata = 0.5f;
    private float lastPathUpdata;
   
    public enum State {Idle,Moving,Attacking}
    private State presentState = State.Idle;

    public int star = 1;

    [Header("AI 전투 오프셋")]
    [SerializeField] private Vector2 attackOffset;

    private Vector2 finalAttackPos;
    private Vector2 lastFinalAttackPos;
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
        if(presentTarget != null && presentTarget.gameObject.activeSelf)
        {
            finalAttackPos = (Vector2)presentTarget.position + attackOffset;

            if(presentState == State.Moving && Vector2.Distance(finalAttackPos,lastFinalAttackPos)> 0.4f)
            {
                RePath();
            }
            lastFinalAttackPos = finalAttackPos;
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
            finalAttackPos = (Vector2)presentTarget.position + attackOffset;
            ChangeState(State.Moving);           
        }       
    }
    public void MoveTarget()
    {
        if(presentTarget == null || !presentTarget.gameObject.activeSelf)
        {
            presentTarget = null;
            path = null;
            ChangeState(State.Idle);           
            return;
        }        
        
        float DistTarget = Vector2.Distance(transform.position, presentTarget.position);     

        if(DistTarget <= data.attackRange)
        {
            path = null;
            ChangeState(State.Attacking);
            return;                
        }

        if(Time.time - lastPathUpdata > pathUpdata)
        {
            RePath();
            lastPathUpdata = Time.time;
        }

        if (path != null && path.Count > 0)
        {
            Vector2 targetPos = new Vector2(path[pathIndex].x - GameManager.Instance.mapOffset.x + 0.5f,
                                        path[pathIndex].y - GameManager.Instance.mapOffset.y + 0.5f);
            transform.position = Vector2.MoveTowards(transform.position, targetPos, data.moveSpeed * Time.deltaTime);

            if (Vector2.Distance(transform.position, targetPos) < 0.1f)
            {
                if (pathIndex < path.Count - 1)
                {
                    pathIndex++;
                }
                else
                {
                    path = null;
                }
            }
        }
        else
        {
            RaycastHit2D hit = Physics2D.Linecast(transform.position, presentTarget.position, obstacleLayer);

            if (hit.collider == null)
            {                
                transform.position = Vector2.MoveTowards(transform.position, finalAttackPos, data.moveSpeed * Time.deltaTime);
            }
            else
            {
                RePath();
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
        Vector2Int start = new Vector2Int(Mathf.FloorToInt(transform.position.x) + GameManager.Instance.mapOffset.x,
                                                    Mathf.FloorToInt(transform.position.y) + GameManager.Instance.mapOffset.y);

        Vector2Int end = new Vector2Int(Mathf.FloorToInt(finalAttackPos.x) + GameManager.Instance.mapOffset.x,
                                        Mathf.FloorToInt(finalAttackPos.y) + GameManager.Instance.mapOffset.y);

        start.x = Mathf.Clamp(start.x, 0, GameManager.Instance.width - 1);
        start.y = Mathf.Clamp(start.y, 0, GameManager.Instance.height - 1);
        end.x = Mathf.Clamp(end.x, 0, GameManager.Instance.width - 1);
        end.y = Mathf.Clamp(end.y, 0, GameManager.Instance.height - 1);

        pathIndex = 0;
        path = Node.FindPath(GameManager.Instance.map, start, end);
    }
    public void AttackTarget()
    {
        if (presentTarget == null || !presentTarget.gameObject.activeSelf)
        {
            presentTarget = null;
            path = null;
            ChangeState(State.Idle);
            return;
        }

        float atkSpeed = currentAttackSpeed;
        int atkPower = currentAttackPower;
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
        if (isUsingSkill) return;
        if(Time.time - lastAttackTime >= 1.0f / atkSpeed)
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








머지오브젝트
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


현재 마지오브젝트

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MergeObject : MonoBehaviour
{
    Character Character;     
    DragController dragController;

    private MergeObject potentialMergeTarget;
    private bool mergeExecuted = false;
    void Awake()
    {
        Character = GetComponent<Character>();
        dragController = GetComponent<DragController>();
    }
    public bool CanMergeWith(MergeObject other)
    {
        if (other == null || other.gameObject == gameObject) return false;
        if (Character == null || other.Character == null) return false;
        return other.Character.data.id == Character.data.id && other.Character.star == Character.star;
    }
    public bool ExecuteMerge(MergeObject target)
    {
        Debug.Log($"<color=magenta>[{gameObject.name}] ExecuteMerge 시작 - myID={GetInstanceID()}, targetID={target.GetInstanceID()}</color>");
        // 중복 병합을 막기 위해 고유 ID가 더 큰 쪽이 항상 병합을 주도
        if (this.GetInstanceID() < target.GetInstanceID()) 
        {
            Debug.Log($"<color=yellow>[{gameObject.name}] ID가 작아서 병합 중단</color>");
            return false;
        }

        int currentStar = Character.star;
        int nextIndex = currentStar;
        var data = Character.data;

        if (data == null || data.Prefabs == null || data.Prefabs.Length <= nextIndex || data.Prefabs[nextIndex] == null)
        {
            Debug.LogError("다음 등급의 유닛 프리팹이 설정되지 않았습니다!");
            return false;
        }
        GameObject nextPrefab = data.Prefabs[nextIndex];
        Transform mergeTile = (target.transform.parent != null) ? target.transform.parent : transform.parent;
        Vector3 spawnPos = (target.transform.position + transform.position) * 0.5f;

        GameObject newUnitObj = Instantiate(nextPrefab, spawnPos, Quaternion.identity);
        if (mergeTile != null) newUnitObj.transform.SetParent(mergeTile, true);

        var newCs = newUnitObj.GetComponent<Character>();
       

        if (newCs != null)
        {
            newCs.star = currentStar + 1;
            newCs.isEnemy = Character.isEnemy;
            newCs.ReSetState();
        }
        var newDc = newUnitObj.GetComponent<DragController>();
        if (newDc != null)
        {
            bool spawnFlag = (mergeTile != null && mergeTile.CompareTag("SpawnPoint"));
            newDc.isSpawnZone = spawnFlag;
            newDc.UpdatePositionAndParent();
            // ★★★ 핵심 수정: 콜라이더 상태 명시적 초기화 ★★★
            newDc.ResetColliderState();

            Debug.Log($"[MergeObject] 새 유닛 {newUnitObj.name} 생성 완료 - isSpawnZone={spawnFlag}");
        }
        else
        {
            Debug.LogError($"<color=red>[{newUnitObj.name}] DragController가 없습니다!</color>");
        }

        if (GameManager.Instance != null)
        {
            List<GameObject> mergedUnits = new List<GameObject> { gameObject, target.gameObject };
            GameManager.Instance.ProcessUnitMerge(mergedUnits, newUnitObj);
        }
        Debug.Log($"<color=magenta>[{gameObject.name}] 병합 완료!</color>");

        Destroy(target.gameObject);
        Destroy(gameObject);

        return true; // 병합 성공
    }  
}

 */