using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TextManager : MonoBehaviour
{

    [SerializeField] private Transform gameOver;
    [SerializeField] private Transform stageclear;
    [SerializeField] private Transform youDead;
    [SerializeField] private Transform restartButton;
    [SerializeField] private Transform[] lifeHeart;
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private List<GameObject> objsHide;
    public List<GameObject> friendlyUnit;
    public List<GameObject> enemyUnit;

    private bool isGameOver;
    private bool isStageClear;
    private bool isYouDead;

    private int stageStartGold;

    private bool _restarting = false;
    private bool _loadingNext = false;    
    void Start()
    {
        if (friendlyUnit == null) friendlyUnit = new List<GameObject>();
        else friendlyUnit.Clear();

        if (enemyUnit == null) enemyUnit = new List<GameObject>();
        else enemyUnit.Clear();

        isGameOver = false;
        isStageClear = false;
        isYouDead = false;

        gameOver.gameObject.SetActive(false);
        stageclear.gameObject.SetActive(false);
        youDead.gameObject.SetActive(false);

        if (GameManager.Instance != null && coinText != null)
            coinText.text = GameManager.Instance.playerGold.ToString("N0");

        UpdateLifeUI();
        StopTime();
    }
    void Update()
    {
        if (GameManager.Instance != null && coinText != null)
            coinText.text = GameManager.Instance.playerGold.ToString("N0");

        if (isGameOver)
        {
            Restart();
        }
        if(isStageClear && !_loadingNext)
        {
            NextGame();            
        }
        if(isYouDead && !_restarting)
        {
            AgainGame();
        }
    }
    public void StartTime()
    {        
        Time.timeScale = 1;
        if (GameManager.Instance != null)
            stageStartGold = GameManager.Instance.playerGold;        

        foreach (GameObject obj in objsHide)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
        Character[] allUnits = FindObjectsOfType<Character>();
        friendlyUnit.Clear();
        enemyUnit.Clear();
        foreach (Character unit in allUnits)
        {            
            bool inBattle = true;
            if (GameManager.Instance != null)
            {
                var p = Vector2Int.RoundToInt((Vector2)unit.transform.position);
                inBattle = GameManager.Instance.IsInsideBattle(p);
            }
            unit.isBattle = inBattle;
            if (unit.dragController != null)
            {
                unit.dragController.enabled = false;
            }
        }
        StartCoroutine(BattleCheck());
    }
    public void StopTime()
    {
        Time.timeScale = 0;

        foreach (GameObject obj in objsHide)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }

        var units = FindObjectsOfType<Character>();
        foreach (var u in units)
        {
            if (u.dragController != null) u.dragController.enabled = true;
            u.isBattle = false;
        }
    }
    void AddCoin(int plusGold = 100)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.playerGold += plusGold;
    }   
    void Restart()
    {
        isGameOver = true;
        if (gameOver) gameOver.gameObject.SetActive(true);
        StopTime();
    }
    void NextGame()
    {
        if (_loadingNext) return;
        _loadingNext = true;

        isStageClear = true;
        stageclear.gameObject.SetActive(true);
        AddCoin(100);

        StopTime();
        StartCoroutine(LoadNextScene());              
    }
    void AgainGame()
    {
        if (_restarting) return;
        _restarting = true;
        isYouDead = true;
        if (GameManager.Instance.lifeCount > 0)
        {
            GameManager.Instance.lifeCount--;
            UpdateLifeUI();
          
            GameManager.Instance.SnapshotUnitsFromScene();
            GameManager.Instance.SaveToJson();

            StartCoroutine(RestartStage());
        }
        else
        {
            isGameOver = true;
            isYouDead = false;
            gameOver.gameObject.SetActive(true);
            _restarting = false;
        }
    }
    public void UpdateLifeUI()
    {
        if (GameManager.Instance == null || lifeHeart == null) return;

        int life = GameManager.Instance.lifeCount;
        for (int i = 0; i < lifeHeart.Length; i++)
        {
            if (lifeHeart[i] == null) continue;
            lifeHeart[i].gameObject.SetActive(i < life);
        }
    }
    IEnumerator LoadNextScene()
    {
        yield return new WaitForSecondsRealtime(2f);

        int currentScene = SceneManager.GetActiveScene().buildIndex;
        int nextScene = currentScene + 1;

        stageclear.gameObject.SetActive(false);

        if (nextScene < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextScene);           
        }
        else
        {
            Debug.Log("Last Scene");
        }
        _loadingNext = false;
    }
    IEnumerator BattleCheck()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(1f);

            int friendCount = 0;
            int enemyCount = 0;

            var chars = FindObjectsOfType<Character>();
            foreach (var cs in chars)
            {
                if (!cs.gameObject.activeInHierarchy) continue;
                
                if (GameManager.Instance != null)
                {
                    var p = Vector2Int.RoundToInt((Vector2)cs.transform.position);
                    if (!GameManager.Instance.IsInsideBattle(p)) continue; // ← 대기실 제외
                }

                if (cs.isEnemy) enemyCount++;
                else friendCount++;
            }
            if (friendCount <= 0 && enemyCount > 0)
            {
                isYouDead = true;
                yield return new WaitForSecondsRealtime(0.5f);
                yield break;
            }

            if (enemyCount <= 0 && friendCount > 0)
            {
                isStageClear = true;
                yield return new WaitForSecondsRealtime(0.5f);
                yield break;
            }
        }       
    }
    IEnumerator RestartStage()
    {
        youDead.gameObject.SetActive(true);
        StopTime();

        yield return new WaitForSecondsRealtime(2.5f);
        GameManager.Instance.playerGold = stageStartGold;

        friendlyUnit.Clear();
        enemyUnit.Clear();

        string currentScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentScene);
    }
    public void RestartButton()
    {
        GameManager.Instance.ResetGameData();
        UpdateLifeUI();
        restartButton.gameObject.SetActive(false);
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
    public void PauseButton()
    {
        Time.timeScale = 0f;
    }
    public void ReproductionButton()
    {
        Time.timeScale = 1f;        
    }
}
