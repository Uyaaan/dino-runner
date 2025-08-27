using UnityEngine;
using TMPro;
using System;

public class Main_UI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI Timer;
    [SerializeField] private TextMeshProUGUI Distance;
    [SerializeField] private Transform player;
    private float timer;
    // private int distance = 0;
    private Vector3 startPos;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startPos = player.position;
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        UI_Update();
        Distance_Update();
    }

    void UI_Update()
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(timer);
        // Distance.text = distance.ToString();
        if (timeSpan.Hours > 0)
        {
            // Show hours if not zero → hh:mm:ss.ms
            Timer.text = string.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);
        }
        else
        {
            // Hide hours → mm:ss.ms
            Timer.text = string.Format("{0:00}:{1:00}.{2:00}",
                timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);
        }
    }

    void Distance_Update()
    {
        // Measure distance from starting point
        float distance = Vector3.Distance(startPos, player.position);

        // Round for readability (e.g., 123.4 m)
        Distance.text = distance.ToString("F1") + " m";
    }
}
