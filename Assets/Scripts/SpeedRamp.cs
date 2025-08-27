using UnityEngine;
using UnityEngine.UI;

public class EndlessRunnerController : MonoBehaviour
{
    [Header("Speed Settings")]
    [SerializeField] private float startSpeed = 7f;  // Faster initial speed
    [SerializeField] private float maxSpeed = 30f;   // Higher ceiling
    [SerializeField] private float speedIncreaseRate = 0.8f; // Faster ramp per second
    [SerializeField] private AnimationCurve speedCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Alternative Speed Progression")]
    [SerializeField] private bool useIntervalIncrease = false;
    [SerializeField] private float speedIncreaseInterval = 8f; // Shorter intervals
    [SerializeField] private float speedIncreaseAmount = 3f;   // Bigger jumps per interval

    [Header("Game State")]
    [SerializeField] private bool isGameActive = true;
    [SerializeField] private float gameTime = 0f;

    [Header("UI References (Optional)")]
    [SerializeField] private Text speedText;
    [SerializeField] private Text timeText;
    [SerializeField] private Text scoreText;

    // Properties
    public float CurrentSpeed { get; private set; }
    public float GameTime => gameTime;
    public bool IsGameActive => isGameActive;

    // Events
    public System.Action<float> OnSpeedChanged;
    public System.Action<float> OnGameTimeChanged;

    // Private
    private float lastSpeedIncreaseTime = 0f;
    private int score = 0;

    void Start()
    {
        InitializeGame();
    }

    void Update()
    {
        if (!isGameActive) return;

        UpdateGameTime();
        UpdateSpeed();
        UpdateScore();
        UpdateUI();
    }

    private void InitializeGame()
    {
        CurrentSpeed = startSpeed;
        gameTime = 0f;
        score = 0;
        lastSpeedIncreaseTime = 0f;

        if (speedCurve.keys.Length < 2)
        {
            speedCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        }

        OnSpeedChanged?.Invoke(CurrentSpeed);
    }

    private void UpdateGameTime()
    {
        gameTime += Time.deltaTime;
        OnGameTimeChanged?.Invoke(gameTime);
    }

    private void UpdateSpeed()
    {
        if (useIntervalIncrease)
        {
            UpdateSpeedByInterval();
        }
        else
        {
            UpdateSpeedContinuous();
        }

        CurrentSpeed = Mathf.Min(CurrentSpeed, maxSpeed);
    }

    private void UpdateSpeedContinuous()
    {
        float previousSpeed = CurrentSpeed;

        float speedProgress = (CurrentSpeed - startSpeed) / (maxSpeed - startSpeed);
        float curveMultiplier = speedCurve.Evaluate(speedProgress);

        float speedIncrease = speedIncreaseRate * Time.deltaTime * (1.2f + curveMultiplier); // +20% ramp
        CurrentSpeed += speedIncrease;

        if (Mathf.Abs(CurrentSpeed - previousSpeed) > 0.01f)
        {
            OnSpeedChanged?.Invoke(CurrentSpeed);
        }
    }

    private void UpdateSpeedByInterval()
    {
        if (gameTime - lastSpeedIncreaseTime >= speedIncreaseInterval)
        {
            CurrentSpeed += speedIncreaseAmount;
            lastSpeedIncreaseTime = gameTime;
            OnSpeedChanged?.Invoke(CurrentSpeed);

            Debug.Log($"Speed increased to: {CurrentSpeed:F1}");
        }
    }

    private void UpdateScore()
    {
        score = Mathf.RoundToInt(gameTime * 15f + (CurrentSpeed - startSpeed) * 8f); // Score grows quicker
    }

    private void UpdateUI()
    {
        if (speedText != null)
            speedText.text = $"Speed: {CurrentSpeed:F1}";

        if (timeText != null)
            timeText.text = $"Time: {gameTime:F1}s";

        if (scoreText != null)
            scoreText.text = $"Score: {score}";
    }

    public void StartGame()
    {
        isGameActive = true;
        InitializeGame();
    }

    public void PauseGame() => isGameActive = false;
    public void ResumeGame() => isGameActive = true;

    public void StopGame()
    {
        isGameActive = false;
        Debug.Log($"Game Over! Final Speed: {CurrentSpeed:F1}, Time: {gameTime:F1}s, Score: {score}");
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        speedIncreaseRate *= multiplier;
    }

    public void AddSpeedBoost(float boostAmount, float duration = 0f)
    {
        if (duration > 0f)
            StartCoroutine(TemporarySpeedBoost(boostAmount, duration));
        else
        {
            CurrentSpeed += boostAmount;
            OnSpeedChanged?.Invoke(CurrentSpeed);
        }
    }

    private System.Collections.IEnumerator TemporarySpeedBoost(float boostAmount, float duration)
    {
        CurrentSpeed += boostAmount;
        OnSpeedChanged?.Invoke(CurrentSpeed);

        yield return new WaitForSeconds(duration);

        CurrentSpeed -= boostAmount;
        OnSpeedChanged?.Invoke(CurrentSpeed);
    }

    public float GetSpeedPercentage()
    {
        return Mathf.Clamp01((CurrentSpeed - startSpeed) / (maxSpeed - startSpeed));
    }

    void OnGUI()
    {
        if (Application.isEditor && isGameActive)
        {
            GUI.Box(new Rect(10, 10, 200, 100), "");
            GUI.Label(new Rect(15, 15, 190, 20), $"Speed: {CurrentSpeed:F2}");
            GUI.Label(new Rect(15, 35, 190, 20), $"Time: {gameTime:F1}s");
            GUI.Label(new Rect(15, 55, 190, 20), $"Progress: {GetSpeedPercentage():P0}");
            GUI.Label(new Rect(15, 75, 190, 20), $"Score: {score}");
        }
    }
}
