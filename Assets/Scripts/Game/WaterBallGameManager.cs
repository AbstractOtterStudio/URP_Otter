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

    [Header("Goal / Celebration")]
    [Tooltip("进球判定的去抖时间（在该时间内重复触发将被忽略）")]
    public float goalDebounce = 1.2f;
    [Tooltip("触发进球队伍 AI 的庆祝动画 Trigger 名")]
    public string celebrateTriggerName = "Celebrate";
    [Tooltip("庆祝动画持续时间（秒），结束后再重置+倒计时开球")]
    public float celebrationDuration = 2.0f;

    [SerializeField] private int scoreBlue = 0;
    [SerializeField] private int scoreRed = 0;
    private float timer;
    [SerializeField] private bool gamePaused = false;
    [SerializeField] private bool gameRunning = false;

    private Dictionary<Component, Vector3> spawnPos = new();
    [SerializeField] private Component player;

    // 去抖/防多次进球
    private float lastGoalAt = -999f;
    private bool goalEnabled = true;  // 只在真正“重新开球”前置为 true
    private Coroutine celebrateCo;

    void Start()
    {
        timer = matchTime;
        Time.timeScale = 0;
        WaterPlayer.isPaused = true;
        WaterPlayerManager.PauseAll();

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

        var pc = FindObjectOfType<PlayerController>();
        if (pc)
        {
            player = pc;
            spawnPos[player] = pc.transform.position;
        }
        if (player) player.gameObject.GetComponent<PlayerMovement>().PlayerPause();

        if (pauseMenu) pauseMenu.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);

        UpdateUI();
        StartCoroutine(StartMatchCountdown());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!gamePaused) PauseGame();
            else ResumeGame();
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
        // —— 去抖 / 禁止重复计分 —— //
        if (!goalEnabled) return;
        if (Time.time - lastGoalAt < goalDebounce) return;
        lastGoalAt = Time.time;

        goalEnabled = false;          // 重置前都不再接受进球
        gameRunning = false;          // 停止计时推进
        WaterPlayerManager.PauseAll();// 冻结AI
        if (player) player.gameObject.GetComponent<PlayerMovement>().PlayerPause();

        // 立即停球避免乱窜（可选）
        if (ball)
        {
            ball.Rb.velocity = Vector3.zero;
            ball.Owner = null; // 清掉AI所有权，防止“吸回去”
        }

        // 计分
        if (team == "Blue") scoreBlue++;
        else scoreRed++;
        UpdateUI();

        // 终局？
        if (scoreBlue >= maxScore || scoreRed >= maxScore)
        {
            EndGame(scoreBlue > scoreRed ? "Blue Wins!" : "Red Wins!");
            return;
        }

        // 庆祝协程（触发动画→等待→再开始倒计时复位/开球）
        if (celebrateCo != null) StopCoroutine(celebrateCo);
        celebrateCo = StartCoroutine(CelebrateAndReset(team));
    }

    IEnumerator CelebrateAndReset(string scoringTeam)
    {
        // 触发进球队伍所有 AI 的庆祝动画
        WaterPlayer[] winners = scoringTeam == "Blue" ? blueTeam : redTeam;
        if (winners != null)
        {
            foreach (var p in winners)
            {
                if (!p || !p.animator) continue;
                // 防止上一帧还留着其它trigger
                p.animator.ResetTrigger(celebrateTriggerName);
                p.animator.SetTrigger(celebrateTriggerName);
            }
        }

        // 等待庆祝动画播放
        float t = Mathf.Max(0f, celebrationDuration);
        if (t > 0f) yield return new WaitForSeconds(t);

        // 进入你原来的“重置+倒计时开球”
        yield return StartCoroutine(ResetPlayWithCountdown());
    }

    IEnumerator ResetPlayWithCountdown()
    {
        ResetPlay();                   // 位置归位 + 暂停

        gameRunning = false;
        if (countdownText) countdownText.gameObject.SetActive(true);

        for (int i = 3; i > 0; i--)
        {
            if (countdownText) countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        if (countdownText) countdownText.gameObject.SetActive(false);

        // 真正开球
        gameRunning = true;
        ResumeGame();

        // 只有在这里重新允许进球
        goalEnabled = true;
    }

    IEnumerator StartMatchCountdown()
    {
        gameRunning = false;
        WaterPlayerManager.PauseAll();
        if (countdownText) countdownText.gameObject.SetActive(true);

        for (int i = 3; i > 0; i--)
        {
            if (countdownText) countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }
        if (countdownText) countdownText.gameObject.SetActive(false);

        Time.timeScale = 1;
        WaterPlayer.isPaused = false;
        WaterPlayerManager.ResumeAll();
        gameRunning = true;
        if (player) player.gameObject.GetComponent<PlayerMovement>().PlayerResume();

        // 开局允许进球
        goalEnabled = true;
        lastGoalAt = Time.time;
    }

    void ResetPlay()
    {
        // 重置球
        if (ball)
        {
            ball.Pos = ballStart.position;
            ball.Rb.velocity = Vector3.zero;
            ball.Owner = null;
        }

        // 重置 AI
        foreach (var p in blueTeam)
        {
            if (!p) continue;
            p.transform.position = spawnPos[p];
            var rb = p.GetComponent<Rigidbody>();
            if (rb) rb.velocity = Vector3.zero;
            // 防御：AI 主动放掉球（见②小补丁）
            p.SafeDropBallOwnership();
        }

        foreach (var p in redTeam)
        {
            if (!p) continue;
            p.transform.position = spawnPos[p];
            var rb = p.GetComponent<Rigidbody>();
            if (rb) rb.velocity = Vector3.zero;
            p.SafeDropBallOwnership();
        }

        // 重置玩家
        if (player)
        {
            var rb = player.GetComponent<Rigidbody>();
            if (rb) rb.velocity = Vector3.zero;
            player.transform.position = spawnPos[player];
        }

        // 暂停到倒计时
        Time.timeScale = 0;
        WaterPlayer.isPaused = true;
        WaterPlayerManager.PauseAll();
        if (player) player.gameObject.GetComponent<PlayerMovement>().PlayerPause();

        // 复位阶段不允许进球
        goalEnabled = false;
    }

    void UpdateUI()
    {
        if (scoreText) scoreText.text = $"{scoreBlue} : {scoreRed}";
        if (timerText) timerText.text = Mathf.CeilToInt(timer).ToString("00");
    }

    void EndGame(string result)
    {
        gameRunning = false;
        goalEnabled = false; // 终局后彻底禁用进球
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
        if (player) player.gameObject.GetComponent<PlayerMovement>().PlayerPause();
        if (pauseMenu) pauseMenu.SetActive(true);
    }

    public void ResumeGame()
    {
        gamePaused = false;
        Time.timeScale = 1;
        WaterPlayer.isPaused = false;
        WaterPlayerManager.ResumeAll();
        if (player) player.gameObject.GetComponent<PlayerMovement>().PlayerResume();
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
