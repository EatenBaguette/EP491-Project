using Unity.VisualScripting;
using UnityEngine;

public class VisualObject : MonoBehaviour
{
    private Renderer renderer;
    private Material material;
    [Header("Physics")]
    [SerializeField] protected Vector3 velocity;
    [SerializeField] protected float friction = 0.95f;
    
    [Header("Bounds")]
    public bool useBounds = false;
    [SerializeField] protected bool wrapBounds = true;
    [SerializeField] protected bool clampBounds = false;

    public Vector3 Velocity => velocity;
    
    private void Awake()
    {
        renderer = GetComponent<Renderer>();
        material = renderer.material;
    }

    void Start()
    {
        material.SetTextureScale("_BaseMap", new Vector2(Random.Range(1f, 2.6f), Random.Range(0.25f, 2.6f)));
    }

    public void ApplyForce(Vector3 force)
    {
        velocity += force;
    }

    public virtual void SetFriction(float newFriction)
    {
        friction = newFriction;
    }

    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }

    public void ResetObject(Vector3 position)
    {
        transform.position = position;
        velocity = Vector3.zero;
    }

    public virtual void Tick(float deltaTime)
    {
        if (!gameObject.activeSelf) return;
        // Apply velocity to position
        transform.position += velocity * deltaTime;

        // Apply simple friction/damping - scaling by deltaTime to be frame-rate independent
        float damping = Mathf.Pow(friction, deltaTime * 60f);
        velocity *= damping;

        if (useBounds)
        {
            HandleBounds();
        }
        
        material.color = Color.Lerp(material.color, Color.white, deltaTime);
    }

    protected virtual void HandleBounds()
    {
        if (!wrapBounds && !clampBounds) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 screenPos = cam.WorldToViewportPoint(transform.position);
        
        if (wrapBounds)
        {
            bool moved = false;
            if (screenPos.x < 0) { screenPos.x += 1; moved = true; }
            else if (screenPos.x > 1) { screenPos.x -= 1; moved = true; }
            
            if (screenPos.y < 0) { screenPos.y += 1; moved = true; }
            else if (screenPos.y > 1) { screenPos.y -= 1; moved = true; }

            if (moved)
            {
                Vector3 worldPos = cam.ViewportToWorldPoint(screenPos);
                worldPos.z = transform.position.z; // Maintain Z
                transform.position = worldPos;
            }
        }
        else if (clampBounds)
        {
            float clampedX = Mathf.Clamp01(screenPos.x);
            float clampedY = Mathf.Clamp01(screenPos.y);
            
            if (clampedX != screenPos.x || clampedY != screenPos.y)
            {
                // If we hit a horizontal boundary, flip and dampen horizontal velocity
                if (clampedX != screenPos.x) velocity.x *= -0.5f;
                // If we hit a vertical boundary, flip and dampen vertical velocity
                if (clampedY != screenPos.y) velocity.y *= -0.5f;

                screenPos.x = clampedX;
                screenPos.y = clampedY;
                
                Vector3 worldPos = cam.ViewportToWorldPoint(screenPos);
                worldPos.z = transform.position.z; // Maintain Z
                transform.position = worldPos;
            }
        }
    }

    public virtual void UpdateObject()
    {
        // Legacy support if needed, or call Tick here if not called by controller
        Tick(Time.deltaTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        gameObject.GetComponent<Renderer>().material.color =
            new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f),1f);
    }
}
