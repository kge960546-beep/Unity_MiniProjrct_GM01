using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMwnuStartButton : MonoBehaviour
{
    public void OnStartButtonClicked()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MapFlat1_1");
    }
}
