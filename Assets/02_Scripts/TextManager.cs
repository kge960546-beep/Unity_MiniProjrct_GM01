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
    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);       
    }
    void Start()
    {
        friendlyUnit.Clear();
        enemyUnit.Clear();

        isGameOver = false;
        isStageClear = false;
        isYouDead = false;

        gameOver.gameObject.SetActive(false);
        stageclear.gameObject.SetActive(false);
        youDead.gameObject.SetActive(false);

        if (GameManager.Instance != null && coinText != null)
            coinText.text = GameManager.Instance.playerGold.ToString("N0");

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
        if(isStageClear)
        {
            NextGame();            
        }
        if(isYouDead)
        {
            AgainGame();
        }
    }
    public void StartTime()
    {        
        Time.timeScale = 1;

        stageStartGold = GameManager.Instance.playerGold;

        foreach (GameObject obj in objsHide)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
        Character[] allUnits = FindObjectsOfType<Character>();
        foreach (Character unit in allUnits)
        {           
            unit.isBattle = true;
           
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
    }
    void AddCoin(int plusGold = 100)
    {
        GameManager.Instance.playerGold += plusGold;       
    }   
    void Restart()
    {
        isGameOver = true;
        gameOver.gameObject.SetActive(true);
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
        int life = GameManager.Instance.lifeCount;

        for (int i = 0; i < lifeHeart.Length; i++)
        {
            if (i < life)
            {
                lifeHeart[i].gameObject.SetActive(true);
            }
            else
            {
                lifeHeart[i].gameObject.SetActive(false);
            }
        }
    }
    IEnumerator LoadNextScene()
    {
        yield return new WaitForSeconds(3f);

        int currentScene = SceneManager.GetActiveScene().buildIndex;
        int nextScene = currentScene + 1;

        stageclear.gameObject.SetActive(false);

        if (nextScene < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextScene);
            yield return null;
            StopTime();
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
            yield return new WaitForSeconds(1f);
            friendlyUnit.RemoveAll(u => u == null);
            enemyUnit.RemoveAll(u => u == null);

            if (friendlyUnit.Count == 0 && enemyUnit.Count > 0)
            {
                isYouDead = true;
                yield return new WaitForSeconds(0.5f);
                yield break;
            }
            if (enemyUnit.Count == 0 && friendlyUnit.Count > 0)
            {
                isStageClear = true;
                yield return new WaitForSeconds(0.5f);
                yield break;
            }
        }       
    }
    IEnumerator RestartStage()
    {
        youDead.gameObject.SetActive(true);
        StopTime();

        yield return new WaitForSeconds(2.5f);
        
        string currentScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentScene);
        yield return null;
        
        GameManager.Instance.playerGold = stageStartGold;
        if (coinText != null)
            coinText.text = GameManager.Instance.playerGold.ToString("N0");

        friendlyUnit.Clear();
        enemyUnit.Clear();
        
        youDead.gameObject.SetActive(false);
        StopTime();

        isYouDead = false;
        _restarting = false;
    }
    public void RestartButton()
    {
        GameManager.Instance.ResetGameData();
        UpdateLifeUI();
        restartButton.gameObject.SetActive(false);
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
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
