using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Manages high scores based on rounds survived and round completion percentage.
/// Score = sum of (round_number * completion_percentage) for each round survived.
/// Completion % = enemies_killed / total_enemies * weighted by remaining health ratio.
/// Persists to PlayerPrefs. Creates a hologram-style panel in the Start Menu scene.
/// </summary>
public class HighScoreManager : MonoBehaviour
{
    public static HighScoreManager Instance { get; private set; }

    [Header("Score Tracking (set at runtime by RoundManager)")]
    public int roundsSurvived = 0;
    public float currentRunScore = 0f;

    [Header("Persistence")]
    [Tooltip("Max number of high scores to store")]
    public int maxHighScores = 5;

    private List<HighScoreEntry> highScores = new List<HighScoreEntry>();
    private const string HIGHSCORE_KEY_PREFIX = "HighScore_";
    private const string HIGHSCORE_COUNT_KEY = "HighScoreCount";

    [System.Serializable]
    public class HighScoreEntry
    {
        public int rounds;
        public float score;
        public string date;
    }

    // Tracks per-round data for score calculation
    private int enemiesThisRound = 0;
    private int enemiesKilledThisRound = 0;
    private float totalHealthRemainingRatio = 0f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        LoadHighScores();
    }

    /// <summary>
    /// Called by RoundManager when a new round starts. Resets per-round tracking.
    /// </summary>
    public void OnRoundStarted(int roundNumber, int totalEnemiesThisRound)
    {
        enemiesThisRound = totalEnemiesThisRound;
        enemiesKilledThisRound = 0;
        totalHealthRemainingRatio = 0f;
    }

    /// <summary>
    /// Called by RoundManager when an enemy is killed. Tracks kill progress.
    /// </summary>
    public void OnEnemyKilled()
    {
        enemiesKilledThisRound++;
    }

    /// <summary>
    /// Called by RoundManager when a round completes. Calculates score for this round.
    /// </summary>
    public void OnRoundCompleted(int roundNumber, int enemiesAliveAtEnd)
    {
        float completionPct = 1f;
        if (enemiesThisRound > 0)
        {
            completionPct = (float)enemiesKilledThisRound / enemiesThisRound;
        }

        float roundScore = roundNumber * completionPct * 100f;
        currentRunScore += roundScore;
        roundsSurvived = roundNumber;

        Debug.Log($"[HighScoreManager] Round {roundNumber} score: {roundScore:F0} (killed {enemiesKilledThisRound}/{enemiesThisRound} = {completionPct*100:F0}%). Total: {currentRunScore:F0}");
    }

    /// <summary>
    /// Called when game is over. Submits the final score.
    /// </summary>
    public void SubmitScore()
    {
        HighScoreEntry entry = new HighScoreEntry
        {
            rounds = roundsSurvived,
            score = currentRunScore,
            date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm")
        };

        highScores.Add(entry);
        highScores.Sort((a, b) => b.score.CompareTo(a.score));

        if (highScores.Count > maxHighScores)
            highScores.RemoveRange(maxHighScores, highScores.Count - maxHighScores);

        SaveHighScores();
        Debug.Log($"[HighScoreManager] Score submitted: Rounds={roundsSurvived}, Score={currentRunScore:F0}");

        currentRunScore = 0f;
        roundsSurvived = 0;
    }

    /// <summary>
    /// Reset current run tracking (e.g. when starting a new game).
    /// </summary>
    public void ResetCurrentRun()
    {
        currentRunScore = 0f;
        roundsSurvived = 0;
        enemiesThisRound = 0;
        enemiesKilledThisRound = 0;
    }

    public List<HighScoreEntry> GetHighScores()
    {
        return new List<HighScoreEntry>(highScores);
    }

    public float GetHighestScore()
    {
        return highScores.Count > 0 ? highScores[0].score : 0f;
    }

    private void SaveHighScores()
    {
        PlayerPrefs.SetInt(HIGHSCORE_COUNT_KEY, highScores.Count);
        for (int i = 0; i < highScores.Count; i++)
        {
            PlayerPrefs.SetInt($"{HIGHSCORE_KEY_PREFIX}{i}_rounds", highScores[i].rounds);
            PlayerPrefs.SetFloat($"{HIGHSCORE_KEY_PREFIX}{i}_score", highScores[i].score);
            PlayerPrefs.SetString($"{HIGHSCORE_KEY_PREFIX}{i}_date", highScores[i].date);
        }
        PlayerPrefs.Save();
    }

    private void LoadHighScores()
    {
        highScores.Clear();
        int count = PlayerPrefs.GetInt(HIGHSCORE_COUNT_KEY, 0);
        for (int i = 0; i < count; i++)
        {
            HighScoreEntry entry = new HighScoreEntry
            {
                rounds = PlayerPrefs.GetInt($"{HIGHSCORE_KEY_PREFIX}{i}_rounds", 0),
                score = PlayerPrefs.GetFloat($"{HIGHSCORE_KEY_PREFIX}{i}_score", 0f),
                date = PlayerPrefs.GetString($"{HIGHSCORE_KEY_PREFIX}{i}_date", "")
            };
            highScores.Add(entry);
        }
        highScores.Sort((a, b) => b.score.CompareTo(a.score));
    }
}
