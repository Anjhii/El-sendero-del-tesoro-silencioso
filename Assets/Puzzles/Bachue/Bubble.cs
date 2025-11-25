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
        if (rb == null || rb.isKinematic) return;

        bool hitBubble = collision.gameObject.CompareTag("Bubble");
        // Añadimos chequeo explicito de Tag "Wall" si tu techo tiene tag Wall, o "BubbleGrid"
        bool hitCeiling = collision.gameObject.CompareTag("BubbleGrid") || collision.gameObject.name.Contains("Top");

        if (hitBubble || hitCeiling)
        {
            StickToGrid(collision);
        }
        else
        {
            // Lógica de rebote normal contra paredes laterales
        }
    }

    private void StickToGrid(Collision collision)
    {
        // 1. Detener físicas
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        // 2. Cambiar Layer de forma SEGURA
        // Solo cambiamos la layer si existe. Si no has creado la layer "GridBubble" en Unity, esto no dará error.
        int layerIndex = LayerMask.NameToLayer("GridBubble");
        if (layerIndex != -1) 
        {
            gameObject.layer = layerIndex;
        }

        // 3. Registrar en el manager
        if (gridManager != null)
        {
            transform.SetParent(gridManager.gridRoot);
            // Usamos el punto de contacto para mayor precisión
            Vector3 contactPoint = collision.GetContact(0).point; 
            gridManager.RegisterBubble(this, contactPoint);
        }
    }
}
