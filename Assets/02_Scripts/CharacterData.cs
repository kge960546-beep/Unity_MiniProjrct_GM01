using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName ="CharacterData",menuName ="OnData")]
public class CharacterData : ScriptableObject 
{
    [Header("기본 정보")]
    public string CharacterName;
    public GameObject CharacterPrefab;

    [Header("기본 능력치")]    
    public int HPMax;
    public int AttackPower;
    public float AttackSpeed;
    public float moveSpeed;
    public float attackRange;

    [Header("장거리 전투 설정")]
    public bool isRenged;
    public GameObject projectilePrefab;
}
