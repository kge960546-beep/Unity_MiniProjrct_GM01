using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StartButton : MonoBehaviour
{
    private Button button;
    public List<GameObject> obfHide;
    private void Start()
    {
        button = GetComponent<Button>();
    }    
    public void Starttime()
    {
        Time.timeScale = 1;

        foreach(GameObject obj in obfHide)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }        
    }
    public void Stoptime()
    {
        Time.timeScale = 0;
    }
    public void hideButton()
    {
        
    }
}
