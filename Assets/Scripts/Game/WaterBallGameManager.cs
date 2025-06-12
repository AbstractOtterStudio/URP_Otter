using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WaterBallGameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public int maxScore = 5;
    public float matchTime = 90f;

    [Header("Refs")]
    public Ball ball;
    public Transform ballStart;
    public WaterPlayer[] blueTeam;
    public WaterPlayer[] redTeam;
    public GameObject pauseMenu;

    [Header("UI")]
    public Text scoreText;
    public Text timerText;
    public Text countdownText;
    public GameObject gameOverPanel;
    public Text gameOverText;

    [SerializeField] private int scoreBlue = 0;
    [SerializeField] private int scoreRed = 0;
    private float timer;
    [SerializeField] private bool gamePaused = false;
    [SerializeField] private bool gameRunning = false;

    private Dictionary<Component, Vector3> spawnPos = new();
    [SerializeField] private Component player;  // 玩家控制对象，可为 MonoBehaviour 或其他

    void Start()
    {
        timer = matchTime;
        Time.timeScale = 0;
        WaterPlayer.isPaused = true;
        WaterPlayerManager.PauseAll();
        // 设置队伍、对手和初始位置
        foreach (var p in blueTeam)
        {
            p.team = new List<WaterPlayer>(blueTeam);
            p.opponents = new List<WaterPlayer>(redTeam);
            spawnPos[p] = p.transform.position;
        }

        foreach (var p in redTeam)
        {
            p.team = new List<WaterPlayer>(redTeam);
            p.opponents = new List<WaterPlayer>(blueTeam);
            spawnPos[p] = p.transform.position;
        }

        // 查找玩家控制对象（带 PlayerController）
        var pc = FindObjectOfType<PlayerController>();
        if (pc)
        {
            player = pc;
            spawnPos[player] = pc.transform.position;
        }
        player.gameObject.GetComponent<PlayerMovement>().PlayerPause();

        if (pauseMenu) pauseMenu.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);

        UpdateUI();
        StartCoroutine(StartMatchCountdown());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!gamePaused)
                PauseGame();
            else
                ResumeGame();
        }

        if (!gamePaused && gameRunning)
        {
            BallStuckDetector.UpdateStuckState(ball, WaterPlayerManager.All);
            timer -= Time.deltaTime;
            UpdateUI();

            if (timer <= 0)
            {
                EndGame(scoreBlue == scoreRed ? "Draw" : (scoreBlue > scoreRed ? "Blue Wins!" : "Red Wins!"));
            }
        }
    }

    public void GoalScored(string team)
    {
        if (team == "Blue") scoreBlue++;
        else scoreRed++;

        UpdateUI();

        if (scoreBlue >= maxScore || scoreRed >= maxScore)
        {
            EndGame(scoreBlue > scoreRed ? "Blue Wins!" : "Red Wins!");
        }
        else
        {
            StartCoroutine(ResetPlayWithCountdown());
        }
    }

    IEnumerator ResetPlayWithCountdown()
    {
        ResetPlay();
        gameRunning = false;
        countdownText.gameObject.SetActive(true);
        for (int i = 3; i > 0; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }
        countdownText.gameObject.SetActive(false);
        gameRunning = true;
        ResumeGame();
    }

    IEnumerator StartMatchCountdown()
    {
        gameRunning = false;
        WaterPlayerManager.PauseAll();
        countdownText.gameObject.SetActive(true);
        for (int i = 3; i > 0; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }
        countdownText.gameObject.SetActive(false);
        Time.timeScale = 1;
        WaterPlayer.isPaused = false;
        WaterPlayerManager.ResumeAll();
        gameRunning = true;
        player.gameObject.GetComponent<PlayerMovement>().PlayerResume();
    }

    void ResetPlay()
    {
        // Reset Ball
        ball.Pos = ballStart.position;
        ball.Rb.velocity = Vector3.zero;
        ball.Owner = null;

        // Reset All WaterPlayers
        foreach (var p in blueTeam)
        {
            p.transform.position = spawnPos[p];
            p.GetComponent<Rigidbody>().velocity = Vector3.zero;
        }

        foreach (var p in redTeam)
        {
            p.transform.position = spawnPos[p];
            p.GetComponent<Rigidbody>().velocity = Vector3.zero;
        }

        // Reset Player if exists
        if (player)
        {
            var rb = player.GetComponent<Rigidbody>();
            if (rb) rb.velocity = Vector3.zero;
            player.transform.position = spawnPos[player];
        }
        Time.timeScale = 0;
        WaterPlayer.isPaused = true;
        WaterPlayerManager.PauseAll();
        player.gameObject.GetComponent<PlayerMovement>().PlayerPause();
    }

    void UpdateUI()
    {
        if (scoreText)
            scoreText.text = $"{scoreBlue} : {scoreRed}";

        if (timerText)
            timerText.text = Mathf.CeilToInt(timer).ToString("00");
    }

    void EndGame(string result)
    {
        gameRunning = false;
        if (gameOverPanel)
        {
            gameOverPanel.SetActive(true);
            if (gameOverText) gameOverText.text = result;
        }
    }

    public void PauseGame()
    {
        gamePaused = true;
        Time.timeScale = 0;
        WaterPlayer.isPaused = true;
        WaterPlayerManager.PauseAll();
        player.gameObject.GetComponent<PlayerMovement>().PlayerPause();
        if (pauseMenu) pauseMenu.SetActive(true);
    }

    public void ResumeGame()
    {
        gamePaused = false;
        Time.timeScale = 1;
        WaterPlayer.isPaused = false;
        WaterPlayerManager.ResumeAll();
        player.gameObject.GetComponent<PlayerMovement>().PlayerResume();
        if (pauseMenu) pauseMenu.SetActive(false);
    }

    public void RestartGame()
    {
        Time.timeScale = 1;
        WaterPlayer.isPaused = false;
        WaterPlayerManager.ResumeAll();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
