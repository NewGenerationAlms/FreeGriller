using System;
using FistVR;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace NGA {
public class FGTimeSystem : MonoBehaviour
{
    public static FGTimeSystem Instance;

    public event Action<DateTime> OnTimeAdvanced; // Only called when time is intentionally changed (e.g., skipping time)
    public event Action<DateTime> OnHourPassed;   // Called when a full hour has passed

    public float timeMultiplier { get; set; } = 24f;  
    public DateTime CurrentTime { get; private set; } = DateTime.Now;  

    private float realTimeElapsed = 0f;
    private int lastReportedHour; // Tracks last hour to detect hourly changes

    public struct Config {
        public string CurrTime;
        public float timeMult;
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        lastReportedHour = CurrentTime.Hour; // Initialize tracking for hourly events
    }

    private void Update()
    {
        realTimeElapsed += Time.deltaTime * timeMultiplier;

        if (realTimeElapsed >= 1f) // Only update when at least 1 second has passed
        {
            int secondsToAdd = Mathf.FloorToInt(realTimeElapsed);
            realTimeElapsed -= secondsToAdd;
            
            // Directly add seconds to time
            CurrentTime = CurrentTime.AddSeconds(secondsToAdd);

            // Check if a new hour has started
            if (CurrentTime.Hour != lastReportedHour)
            {
                lastReportedHour = CurrentTime.Hour;
                OnHourPassed?.Invoke(CurrentTime); // Notify systems of hour change
            }
        }
    } 

    public void AdvanceTime(int seconds)
    {
        CurrentTime = CurrentTime.AddSeconds(seconds);
        OnTimeAdvanced?.Invoke(CurrentTime); // Notify other systems of manual time advancement
    }

    public DateTime GetInGameTimeAfterRealDuration(TimeSpan realDuration)
    {
        DateTime inGameNow = CurrentTime; // Current in-game time
        double realSeconds = realDuration.TotalSeconds;
        double inGameSeconds = realSeconds * timeMultiplier; // Convert to in-game time

        return inGameNow.AddSeconds(inGameSeconds);
    }

    // Inputs are both in-game times. Returns the real-world time until the target time.
    public TimeSpan CalculateRealTimeUntil(DateTime startTime, DateTime targetTime)
    {
        if (targetTime <= startTime)
            return TimeSpan.Zero; // Contract already expired

        double inGameSeconds = (targetTime - startTime).TotalSeconds; // Total in-game seconds
        double realSeconds = inGameSeconds / timeMultiplier; // Convert to real-world seconds

        return TimeSpan.FromSeconds(realSeconds); // Convert to a TimeSpan for easy formatting
    }

    // ---- SAVE & LOAD SYSTEM ----\
    public void InitFromConfig(Config config) {
        CurrentTime = LoadGameTime(config.CurrTime);
        timeMultiplier = config.timeMult;
        lastReportedHour = CurrentTime.Hour;
    }
    public Config GetConfig() {
        return new Config {
            CurrTime = SaveGameTime(),
            timeMult = timeMultiplier,
        };
    }
    public string SaveGameTime()
    {
        return CurrentTime.ToString("o"); // ISO 8601 format (e.g., "2025-02-16T12:34:56.789Z")
    }

    private DateTime LoadGameTime(string dateTimeString)
    {
        if (string.IsNullOrEmpty(dateTimeString)) return DateTime.Now;

        DateTime res = DateTime.Parse(dateTimeString, null, System.Globalization.DateTimeStyles.RoundtripKind);
        if (res == null) return DateTime.Now;
        return res;
    }
}
} // namespace NGA