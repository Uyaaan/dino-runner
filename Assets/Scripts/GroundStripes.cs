using UnityEngine;

public class GroundStripes : MonoBehaviour
{
    [Header("Road Settings")]
    public int segmentCount = 3;          // how many road chunks to keep active
    public float segmentLength = 50f;     // how long each road chunk is
    public float roadWidth = 6f;
    public float roadThickness = 0.1f;
    public Material roadMaterial;

    [Header("Stripe Settings")]
    public float stripeSpacing = 5f;
    public float stripeWidth = 0.2f;
    public float stripeThickness = 0.02f;
    public float stripeLength = 4f;
    public float stripeYOffset = 0.01f;
    public Material stripeMaterial;

    [Header("References")]
    public Transform player;  // player to follow

    private GameObject[] roadSegments;
    private float roadStartZ;

    void Start()
    {
        if (player == null)
        {
            Debug.LogError("EndlessRoad: Player reference is missing.");
            enabled = false;
            return;
        }

        roadSegments = new GameObject[segmentCount];
        roadStartZ = player.position.z;

        for (int i = 0; i < segmentCount; i++)
        {
            roadSegments[i] = CreateRoadSegment(i);
        }
    }

    void Update()
    {
        float playerZ = player.position.z;

        for (int i = 0; i < roadSegments.Length; i++)
        {
            GameObject seg = roadSegments[i];
            float segEndZ = seg.transform.position.z + segmentLength / 2f;

            // if this segment is behind the player, move it forward
            if (segEndZ < playerZ - segmentLength / 2f)
            {
                float newZ = seg.transform.position.z + segmentCount * segmentLength;
                seg.transform.position = new Vector3(seg.transform.position.x, seg.transform.position.y, newZ);
            }
        }
    }

    GameObject CreateRoadSegment(int index)
    {
        GameObject seg = new GameObject($"RoadSegment_{index}");
        seg.transform.SetParent(transform, false);

        float segZ = roadStartZ + index * segmentLength;
        seg.transform.position = new Vector3(0, 0, segZ);

        // --- Road base (with collider) ---
        var road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = "Road";
        road.transform.SetParent(seg.transform, false);
        road.transform.localScale = new Vector3(roadWidth, roadThickness, segmentLength);
        road.transform.localPosition = Vector3.zero;

        // apply material
        if (roadMaterial)
            road.GetComponent<MeshRenderer>().sharedMaterial = roadMaterial;

        // Keep collider on road for physics. Make sure it is non-trigger.
        var roadCollider = road.GetComponent<BoxCollider>();
        if (roadCollider == null)
            roadCollider = road.AddComponent<BoxCollider>();
        roadCollider.isTrigger = false;
        // Collider matches the primitive scale by default; if needed, set center/size:
        roadCollider.center = Vector3.zero;
        roadCollider.size = Vector3.one; // primitive cube's collider uses local scale, so this is fine

        // Optionally mark the road object for identification
        road.tag = "Ground"; // ensure this tag exists or remove this line

        // --- Stripes (no collider) ---
        int stripeCount = Mathf.CeilToInt(segmentLength / stripeSpacing);
        for (int i = 0; i < stripeCount; i++)
        {
            var stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.name = $"Stripe_{i}";
            stripe.transform.SetParent(seg.transform, false);

            float zPos = -segmentLength / 2f + i * stripeSpacing;
            stripe.transform.localPosition = new Vector3(0, stripeYOffset + roadThickness * 0.5f + 0.001f, zPos);
            stripe.transform.localScale = new Vector3(stripeWidth, stripeThickness, stripeLength);

            if (stripeMaterial)
            {
                var r = stripe.GetComponent<MeshRenderer>();
                r.sharedMaterial = stripeMaterial;
            }

            // remove collider to keep things lightweight
            Destroy(stripe.GetComponent<BoxCollider>());
        }

        return seg;
    }
}
