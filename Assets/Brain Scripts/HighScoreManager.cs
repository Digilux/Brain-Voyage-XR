using System;
using System.Collections.Generic;
using UnityEngine;

public class HighScoreManager : MonoBehaviour
{
    public static HighScoreManager Instance { get; private set; }

    [SerializeField] private int maxEntries = 10;
    private const string PrefsKey = "HighScores";

    [Serializable]
    public class Entry
    {
        public int score;
        public string initials;
        public string difficulty;
    }

    [Serializable]
    private class SaveData { public List<Entry> entries = new(); }


    public List<Entry> Entries { get; private set; } = new();
    public event Action OnHighScoresChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    public bool SubmitScore(int score, string initials, string difficulty = "")
    {
        var entry = new Entry
        {
            score = score,
            initials = initials.ToUpper(),
            difficulty = difficulty        // ADD THIS
        };

        Entries.Add(entry);
        Entries.Sort((a, b) => b.score.CompareTo(a.score));
        if (Entries.Count > maxEntries)
            Entries.RemoveRange(maxEntries, Entries.Count - maxEntries);

        Save();
        OnHighScoresChanged?.Invoke();
        return Entries.Contains(entry);
    }

    public void ClearScores()
    {
        Entries.Clear();
        PlayerPrefs.DeleteKey(PrefsKey);
        OnHighScoresChanged?.Invoke();
    }

    private void Save()
    {
        var data = new SaveData { entries = Entries };
        PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    private void Load()
    {
        string json = PlayerPrefs.GetString(PrefsKey, "");
        if (!string.IsNullOrEmpty(json))
            Entries = JsonUtility.FromJson<SaveData>(json)?.entries ?? new();
    }
}
