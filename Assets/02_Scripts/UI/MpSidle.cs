using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MpSidle : MonoBehaviour
{
    public CharacterData data;
    public int currentMp;
    public BarUI barUI;
    private void Start()
    {
        if (data == null) return;

        currentMp = 0;
        if(barUI != null && data != null)
        {
            barUI.UpdateMpBar(currentMp, data.MPMax);
        }        
    }
    public void UseMana(int amount)
    {
        currentMp -= amount;
        if (currentMp < 0) currentMp = 0;
        barUI.UpdateMpBar(currentMp, data.MPMax);
    }
    public void RegenerateMana(int amount)
    {
        currentMp += amount;
        if (currentMp > data.MPMax) currentMp = data.MPMax;
        barUI.UpdateMpBar(currentMp, data.MPMax);
    }
}
