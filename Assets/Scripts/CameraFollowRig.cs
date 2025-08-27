using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollowRig : MonoBehaviour
{
    [SerializeField] private Transform target;               // The player
    [SerializeField] private Vector3 offset = new Vector3(0f, 3.2f, -7.5f);
    [SerializeField] private float followLerp = 8f;          // How smooth the camera follows

    [Header("FOV Kick")]
    [SerializeField] private float baseFov = 60f;            // Default field of view
    [SerializeField] private float maxFov = 72f;             // Max FOV when at top speed

    private Camera cam;
    private RunnerController runner;

    public bool isGameOver = false;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (target != null) runner = target.GetComponent<RunnerController>();
        cam.fieldOfView = baseFov;
    }

    void LateUpdate()
    {
        if (!target) return;

        // Smooth position follow
        Vector3 desired = target.position + target.TransformVector(offset);
        transform.position = Vector3.Lerp(
            transform.position,
            desired,
            1f - Mathf.Exp(-followLerp * Time.deltaTime)
        );

        // Smooth rotation to look at player
        Vector3 lookPoint = target.position + Vector3.up * 1.2f;
        Quaternion lookRot = Quaternion.LookRotation(lookPoint - transform.position);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            lookRot,
            1f - Mathf.Exp(-followLerp * Time.deltaTime)
        );

        // FOV adjustment based on player speed
        if (runner != null && !isGameOver)
        {
            float t = Mathf.Clamp01(runner.Speed01);
            cam.fieldOfView = Mathf.Lerp(baseFov, maxFov, t);
        }
    }
}