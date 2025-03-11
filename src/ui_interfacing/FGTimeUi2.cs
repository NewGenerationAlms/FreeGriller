using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NGA {
public class FGTimeUi2 : MonoBehaviour {

	[SerializeField] public Text timeText;

    private void Start()
    {
        // Call UpdateTimeDisplay every 1 second, starting after 1 second delay
        InvokeRepeating("UpdateTimeDisplay", 1f, 1f);
    }

    private void UpdateTimeDisplay()
    {
        if (NGA.FGTimeSystem.Instance != null && timeText != null)
        {
            DateTime utcTime = NGA.FGTimeSystem.Instance.CurrentTime;
            DateTime localTime = utcTime.ToLocalTime();
            timeText.text = localTime.ToString("MM/dd/yy HH:mm:ss");
        }
    }

    // Optionally stop InvokeRepeating when no longer needed
    private void OnDisable()
    {
        CancelInvoke("UpdateTimeDisplay");
    }
}
} // namespace NGA
