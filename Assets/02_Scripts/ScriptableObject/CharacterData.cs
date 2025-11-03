using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SkillClassification
{
    skillEffectOn,
    justPrefab
}
[CreateAssetMenu(fileName = "CharacterData", menuName = "OnData")]
public class CharacterData : ScriptableObject 
{
    public int id;
    

    [Header("기본 정보")]
    public string CharacterName;
    public GameObject[] Prefabs;

    [Header("기본 능력치")]    
    public int HPMax;
    public int AttackPower;
    public float AttackSpeed;
    public float moveSpeed;
    public float attackRange;
    public int MPMax;
    public int MPRefill;

    [Header("스킬")]
    public int skillDamage;

    [Header("장거리 전투 설정")]
    public bool isRanged;
    public SkillClassification SkillClassification;
    public GameObject projectilePrefab; // 기본공격 발사체
    public GameObject skillEffectPrefab; // 즉발형 스킬 발사체
    public GameObject skillprojectilePrefab; // 기본공경같은 스킬 발사체
}
