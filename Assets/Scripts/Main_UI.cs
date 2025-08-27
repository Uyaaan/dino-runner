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
    [SerializeField] private AudioSource Music_Source_Reverb;
    [SerializeField] private AudioClip[] Music_Clips;
    [SerializeField] private AudioSource SFX_GameOver;
    [SerializeField] private float[] Music_StartTimes;
    [SerializeField] private float[] Music_PlayTimes;
    public bool Countdown_Finished { get; private set; } = false;
    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField] private Camera mainCam;
    [SerializeField] private float zoomAmount = 30f; // target FOV
    [SerializeField] private float zoomDuration = 0.2f;
    [SerializeField] private TextMeshProUGUI GameOver_Distance;
    [SerializeField] private TextMeshProUGUI GameOver_Time;
    [SerializeField] private GameObject Group_GameProper;
    [SerializeField] private GameObject Group_GameOver;
    private float timer;
    // private int distance = 0;
    private Vector3 startPos;

    public bool isGameOver { get; private set; } = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startPos = player.position;
        StartCoroutine(PlayCountdown());
        Group_GameProper.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Countdown_Finished)
        {
            if (!isGameOver)
            {
                timer += Time.deltaTime;
                UI_Update();
                Distance_Update();
            }
            
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            GameOver();
        }

    }

    IEnumerator PlayCountdown()
    {
        // Choose a random track
        int RandomIndex = UnityEngine.Random.Range(0, Music_Clips.Length);
        Music_Source.clip = Music_Clips[RandomIndex];
        Music_Source_Reverb.clip = Music_Clips[RandomIndex];
        Music_Source.time = Music_StartTimes[RandomIndex]; // jump to drop or wherever
        Music_Source_Reverb.time = Music_StartTimes[RandomIndex]; // jump to drop or wherever

        float countdownStartTime = Time.time; // record absolute start of countdown
        Countdown_Image.enabled = true;

        for (int a = 0; a < Countdown_Sprites.Length; a++)
        {
            Countdown_Image.sprite = Countdown_Sprites[a];

            // Check if it's time to start music based on Music_PlayTimes
            float elapsed = Time.time - countdownStartTime;
            if (elapsed >= Music_PlayTimes[RandomIndex] && !Music_Source.isPlaying)
            {
                Music_Source.Play();
            }

            if (a == 3)
            {
                playerController.BeginRun();
                Countdown_Finished = true;
                Group_GameProper.SetActive(true);
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

    public void GameOver()
    {
        // AudioSource.PlayClipAtPoint(SFX_GameOver, Camera.main.transform.position);
        isGameOver = true;
        Group_GameProper.SetActive(false);
        Group_GameOver.SetActive(true);
        TimeSpan timeSpan = TimeSpan.FromSeconds(timer);
        float distance = Vector3.Distance(startPos, player.position);
        GameOver_Distance.text = distance.ToString("F1") + " meters";
        GameOver_Time.text = string.Format("{0:00}:{1:00}.{2:00}",
                    timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);
        SFX_GameOver.Play();
        FindFirstObjectByType<CameraFollowRig>().isGameOver = true;
        StartCoroutine(TransitionToReverb());
        StartCoroutine(CameraZoom());
    }

    IEnumerator CameraZoom()
    {
        float startFOV = mainCam.fieldOfView;
        float elapsed = 0f;

        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / zoomDuration;

            // Ease-in-out but weighted toward the beginning (fast zoom, gentle stop)
            t = Mathf.SmoothStep(0f, 1f, t);
            // t = Mathf.Pow(t, 0.5f); // stronger ease, faster start

            mainCam.fieldOfView = Mathf.Lerp(startFOV, zoomAmount, t);
            yield return null;
        }

        mainCam.fieldOfView = zoomAmount;
    }


    IEnumerator TransitionToReverb()
    {
        float elapsed = 0f;
        // float startVolume = Music_Source.volume;
        float startVolume = 0.2f;

        Music_Source_Reverb.time = Music_Source.time; // sync time positions
        Music_Source_Reverb.Play();

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            Music_Source.volume = Mathf.Lerp(startVolume, 0f, t);
            Music_Source_Reverb.volume = Mathf.Lerp(0f, startVolume, t);

            yield return null;
        }

        Music_Source.Stop();
    }
}
