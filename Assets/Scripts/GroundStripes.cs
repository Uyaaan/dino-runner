using UnityEngine;

public class GroundStripes : MonoBehaviour
{
    [Header("Stripe Settings")]
    public float spacing = 5f;       // distance between stripes
    public int count = 40;           // how many to place forward
    public float width = 0.2f;       // x scale
    public float thickness = 0.02f;  // y scale
    public float length = 4f;        // z scale
    public float yOffset = 0.01f;    // lift above plane to avoid z-fighting
    public Material stripeMaterial;  // assign a white material

    [Header("Placement")]
    public float startZ = -10f;      // start a bit behind the player
    public float x = 0f;             // center stripe in lane

    void Start()
    {
        for (int i = 0; i < count; i++)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Stripe_{i}";
            go.transform.SetParent(transform, worldPositionStays: false);
            go.transform.localPosition = new Vector3(x, yOffset, startZ + i * spacing);
            go.transform.localScale = new Vector3(width, thickness, length);

            if (stripeMaterial)
            {
                var r = go.GetComponent<MeshRenderer>();
                r.sharedMaterial = stripeMaterial;
            }

            // remove collider to keep things lightweight
            Destroy(go.GetComponent<BoxCollider>());
        }
    }
}