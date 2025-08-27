using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;


public class Main_UI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI Timer;
    [SerializeField] private TextMeshProUGUI Distance;
    [SerializeField] private Transform player;
    [SerializeField] private Image Countdown_Image;
    [SerializeField] private Sprite[] Countdown_Sprites;
    [SerializeField] private float Countdown_Delay = 1f;
    [SerializeField] private RunnerController playerController;
    [SerializeField] private AudioSource Music_Source;
    [SerializeField] private AudioClip[] Music_Clips;
    public bool Countdown_Finished { get; private set; } = false;
    private float timer;
    // private int distance = 0;
    private Vector3 startPos;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startPos = player.position;
        StartCoroutine(PlayCountdown());
    }

    // Update is called once per frame
    void Update()
    {
        if (Countdown_Finished)
        {
            timer += Time.deltaTime;
            UI_Update();
            Distance_Update();
        }
        
    }

    IEnumerator PlayCountdown()
    {
        Countdown_Image.enabled = true;
        for (int a = 0; a < Countdown_Sprites.Length; a++)
        {
            Countdown_Image.sprite = Countdown_Sprites[a];
            if (a == 3)
            {
                playerController.BeginRun();
                Countdown_Finished = true;
                int RandomIndex = UnityEngine.Random.Range(0, Music_Clips.Length);
                Music_Source.clip = Music_Clips[RandomIndex];
                Music_Source.Play();
            }
            yield return new WaitForSeconds(Countdown_Delay);
        }

        Countdown_Image.enabled = false;
        
       
    }

    void UI_Update()
    {
        if (Countdown_Finished)
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
        
    }

    void Distance_Update()
    {
        // Measure distance from starting point
        float distance = Vector3.Distance(startPos, player.position);

        // Round for readability (e.g., 123.4 m)
        Distance.text = distance.ToString("F1") + " m";
    }
}
