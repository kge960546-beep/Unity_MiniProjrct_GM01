using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BarUI : MonoBehaviour
{
    public Slider hpSlider;
    public Slider mpSlider;
    public void UpdateHpBar(int currentHp, int maxHp)
    {
        if (hpSlider == null) return;
        if (maxHp <= 0) return;
        hpSlider.value = (float)currentHp / maxHp;
    }
    public void UpdateMpBar(int currentMp, int maxMp)
    {
        if (mpSlider == null) return;
        if (maxMp <= 0) return;

        mpSlider.value = (float)currentMp / maxMp;
    }
}
