using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BarUI : MonoBehaviour
{
    public Slider hpSlider;
    public void UpdateHpBar(int currentHp, int maxHp)
    {
        hpSlider.value = (float)currentHp / maxHp;
    }
}
