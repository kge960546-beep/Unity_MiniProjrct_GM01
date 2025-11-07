using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitButton : MonoBehaviour
{    
    public void ExitGame()
    {
        // 에디터에서는 플레이 모드를 중지하고, 빌드된 프로그램에서는 게임을 종료합니다.
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
