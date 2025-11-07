using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HpSidle : MonoBehaviour
{
    public CharacterData data;
    public int currentHp;

    public Action<int, int> onHpChange;
    public BarUI barUI;

    private void Start()
    {
        currentHp = data.HPMax;
        onHpChange += barUI.UpdateHpBar;
        onHpChange?.Invoke(currentHp, data.HPMax);
    }  
    public void TakeDamage(int damage)
    {
        currentHp -= damage;
        if (currentHp < 0) currentHp = 0;
        onHpChange?.Invoke(currentHp, data.HPMax);

        if (currentHp <= 0)
        {
            Die();
        }
    }
    private void Die()
    {               
        Destroy(gameObject);
    }



}
