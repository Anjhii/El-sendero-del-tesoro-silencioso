/* using UnityEngine;

[RequireComponent(typeof(Renderer), typeof(Rigidbody), typeof(SphereCollider))]
public class Bubble : MonoBehaviour
{
    [Header("Visual / Color")]
    public int colorId = 0;
    public Material[] colorMaterials;

    [Header("Behaviour")]
    public float maxLifetime = 10f; // tiempo para auto-destruir si queda volando
    public string bubbleTag = "Bubble";

    [Header("References (auto)")]
    public BubbleGridManager gridManager;

    Rigidbody rb;
    Renderer rend;
    SphereCollider sphereCol;
    float birthTime;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        rb = GetComponent<Rigidbody>();
        sphereCol = GetComponent<SphereCollider>();

        // asegurar physics material si no está
        if (sphereCol != null && sphereCol.material == null)
        {
            // deja que el diseñador asigne BubbleBounce manualmente; aquí no sobreescribimos
        }

        // tag y layer deberían asignarse al prefab pero por seguridad:
        if (!string.IsNullOrEmpty(bubbleTag))
            gameObject.tag = bubbleTag;

        if (gridManager == null)
            gridManager = FindObjectOfType<BubbleGridManager>();

        birthTime = Time.time;
    }

    void Start()
    {
        if (colorMaterials != null && colorMaterials.Length > 0)
        {
            SetColorById(colorId);
        }
    }

    void Update()
    {
        // autodestruye si queda demasiado tiempo sin pegarse (evita basura)
        if (Time.time - birthTime > maxLifetime && rb != null && !rb.isKinematic)
        {
            Destroy(gameObject);
        }
    }

    public void SetRandomColor()
    {
        if (colorMaterials == null || colorMaterials.Length == 0) return;
        colorId = Random.Range(0, colorMaterials.Length);
        SetColorById(colorId);
    }

    public void SetColorById(int id)
    {
        colorId = Mathf.Clamp(id, 0, Mathf.Max(0, colorMaterials.Length - 1));
        if (rend != null && colorMaterials != null && colorMaterials.Length > 0)
            rend.material = colorMaterials[colorId];
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Se "pega" solo si golpea otra burbuja o el grid (y con velocidad suficiente)
        if (rb == null) return;

        // verificar si ya está kinematic (ya pegada)
        if (rb.isKinematic) return;

        // si choca con una burbuja o con el grid root, se pega
        bool hitBubble = collision.gameObject.CompareTag("Bubble");
        bool hitGrid = collision.gameObject.CompareTag("BubbleGrid");

        if ((hitBubble || hitGrid) && rb.velocity.magnitude > 0.05f)
        {
            // detener física y parentear al grid
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;

            // Opcional: snap a posición cercana (dejamos que GridManager lo haga)
            if (gridManager != null)
            {
                transform.SetParent(gridManager.gridRoot, true);
                gridManager.RegisterBubble(this);
            }
            else
            {
                // fallback: sólo parent y marcar
                transform.SetParent(null, true);
            }
        }
    }
} */

using UnityEngine;

[RequireComponent(typeof(Renderer), typeof(Rigidbody), typeof(SphereCollider))]
public class Bubble : MonoBehaviour
{
    public int colorId;
    public Material[] colorMaterials;
    public float maxLifetime = 10f;
    public string bubbleTag = "Bubble";
    public BubbleGridManager gridManager;

    Rigidbody rb;
    Renderer rend;
    float birthTime;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        rb = GetComponent<Rigidbody>();

        gameObject.tag = bubbleTag;

        if (gridManager == null)
            gridManager = FindObjectOfType<BubbleGridManager>();

        birthTime = Time.time;
    }

    void Start()
    {
        if (colorMaterials != null && colorMaterials.Length > 0)
            SetColorById(colorId);
    }

    void Update()
    {
        if (!rb.isKinematic && Time.time - birthTime > maxLifetime)
            Destroy(gameObject);
    }

    public void SetRandomColor()
    {
        colorId = Random.Range(0, colorMaterials.Length);
        SetColorById(colorId);
    }

    public void SetColorById(int id)
    {
        rend.material = colorMaterials[id];
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (rb == null) return;
        if (rb.isKinematic) return;

        bool hitBubble = collision.gameObject.CompareTag("Bubble");
        bool hitGrid = collision.gameObject.CompareTag("BubbleGrid");

        // 🔥 NO PEGAR si la velocidad es casi cero (colisiones falsas)
        if ((hitBubble || hitGrid) && rb.velocity.magnitude > 0.05f)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;

            transform.position += collision.contacts[0].normal * 0.001f;

            transform.SetParent(gridManager.gridRoot);
            gridManager.RegisterBubble(this);
        }
    }


}
