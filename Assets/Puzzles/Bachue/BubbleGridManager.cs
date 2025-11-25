using System.Collections.Generic;
using UnityEngine;

public class BubbleGridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public Transform gridRoot;
    public GameObject bubblePrefab;
    public int rows = 6;
    public int columns = 8;
    public float bubbleSpacing = 0.18f;
    public LayerMask bubbleLayer;

    [Header("Matching")]
    public float neighborRadius = 0.25f;
    public int minMatchSize = 3;
    public int pointsPerBubble = 10;

    [Header("References")]
    public ScoreManagerBubble scoreManager;

    private List<Bubble> allBubbles = new List<Bubble>();

    private void Awake()
    {
        if (gridRoot == null) 
            gridRoot = this.transform;
    }

    private void Start()
    {
        GenerateInitialGrid();
    }

    public void GenerateInitialGrid()
    {
        if (bubblePrefab == null) return;

        // Limpiar grid previa
        for (int i = gridRoot.childCount - 1; i >= 0; i--)
            DestroyImmediate(gridRoot.GetChild(i).gameObject);

        allBubbles.Clear();

        Vector3 origin = gridRoot.position;

        float startX = -(columns - 1) * 0.5f * bubbleSpacing;
        float startY = (rows - 1) * 0.5f * bubbleSpacing;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                float offsetX = (r % 2 == 0) ? 0f : bubbleSpacing * 0.5f;

                Vector3 pos = origin + new Vector3(
                    startX + c * bubbleSpacing + offsetX,
                    startY - r * bubbleSpacing,
                    0f
                );

                GameObject go = Instantiate(bubblePrefab, pos, Quaternion.identity, gridRoot);
                Bubble b = go.GetComponent<Bubble>();

                if (b != null)
                {
                    b.gridManager = this;
                    if (b.colorMaterials != null && b.colorMaterials.Length > 0)
                        b.SetRandomColor();
                }

                Rigidbody rb = go.GetComponent<Rigidbody>();
                if (rb != null)
                    rb.isKinematic = true;

                allBubbles.Add(b);
            }
        }
    }

    public void RegisterBubble(Bubble newBubble)
    {
        if (newBubble == null) return;

        if (!allBubbles.Contains(newBubble))
            allBubbles.Add(newBubble);

        SnapToNearest(newBubble);
        CheckForGroups(newBubble);
    }

    void SnapToNearest(Bubble bubble)
    {
        float stepX = bubbleSpacing;
        float stepY = bubbleSpacing; // O bubbleSpacing * 0.866f si es hexagonal estricto

        Vector3 localPos = gridRoot.InverseTransformPoint(bubble.transform.position);

        // 1. Calcular la posición ideal matemática
        float snapX = Mathf.Round(localPos.x / stepX) * stepX;
        float snapY = Mathf.Round(localPos.y / stepY) * stepY;
        
        // Ajuste para grilla hexagonal (filas alternas)
        // Si usas grilla cuadrada simple, puedes ignorar este bloque 'if'
        int gridRow = Mathf.RoundToInt(localPos.y / stepY);
        if (Mathf.Abs(gridRow) % 2 == 1) // Si es fila impar, desplazar X
        {
            float offsetX = stepX * 0.5f;
            // Recalcular snapX considerando el offset
            snapX = (Mathf.Round((localPos.x - offsetX) / stepX) * stepX) + offsetX;
        }

        Vector3 targetPos = gridRoot.TransformPoint(new Vector3(snapX, snapY, 0f));

        // 2. VERIFICACIÓN DE OCUPACIÓN
        // Revisamos si ya hay UNA OTRA bola en esa posición (radio pequeño para ser precisos)
        Collider[] hits = Physics.OverlapSphere(targetPos, bubbleSpacing * 0.4f, bubbleLayer);
        bool isOccupied = false;
        foreach(var hit in hits)
        {
            if (hit.gameObject != bubble.gameObject) // Si choca con algo que no soy yo mismo
            {
                isOccupied = true;
                break;
            }
        }

        // 3. Si está ocupado, buscar el vecino libre más cercano
        if (isOccupied)
        {
            targetPos = FindClosestFreeHexSpot(bubble.transform.position);
        }

        // Aplicar posición final
        bubble.transform.position = targetPos;
    }

// Método auxiliar para buscar hueco libre alrededor
Vector3 FindClosestFreeHexSpot(Vector3 currentWorldPos)
{
    // Definir las 6 direcciones hexagonales (aprox)
    // Si tu grilla es cuadrada, usa Vector3.up, down, left, right
    Vector3[] directions = new Vector3[]
    {
        new Vector3(1, 0, 0), new Vector3(-1, 0, 0),
        new Vector3(0.5f, 0.866f, 0), new Vector3(-0.5f, 0.866f, 0),
        new Vector3(0.5f, -0.866f, 0), new Vector3(-0.5f, -0.866f, 0)
    };

    float minDist = float.MaxValue;
    Vector3 bestSpot = currentWorldPos;

    // Buscar en los 6 vecinos teóricos cuál está vacío
    foreach (Vector3 dir in directions)
    {
        // Posición candidata basada en el espaciado
        Vector3 candidate = currentWorldPos + (dir * bubbleSpacing);
        
        // "Snap" de la candidata a la grilla para asegurar alineación perfecta
        Vector3 local = gridRoot.InverseTransformPoint(candidate);
        float sX = Mathf.Round(local.x / bubbleSpacing) * bubbleSpacing;
        float sY = Mathf.Round(local.y / bubbleSpacing) * bubbleSpacing; 
        // Nota: Si usas offset hexagonal, aplica la misma lógica de arriba aquí
        
        Vector3 alignedCandidate = gridRoot.TransformPoint(new Vector3(sX, sY, 0));

        // Chequear si está libre
        if (!Physics.CheckSphere(alignedCandidate, bubbleSpacing * 0.4f, bubbleLayer))
        {
            float d = Vector3.Distance(currentWorldPos, alignedCandidate);
            if (d < minDist)
            {
                minDist = d;
                bestSpot = alignedCandidate;
            }
        }
    }
    return bestSpot;
}

    private void CheckForGroups(Bubble startBubble)
    {
        List<Bubble> group = new List<Bubble>();
        Queue<Bubble> queue = new Queue<Bubble>();
        HashSet<Bubble> visited = new HashSet<Bubble>();

        queue.Enqueue(startBubble);
        visited.Add(startBubble);

        while (queue.Count > 0)
        {
            Bubble current = queue.Dequeue();
            group.Add(current);

            Collider[] hits = Physics.OverlapSphere(
                current.transform.position,
                neighborRadius,
                bubbleLayer
            );

            foreach (Collider hit in hits)
            {
                Bubble neighbor = hit.GetComponent<Bubble>();
                if (neighbor != null &&
                    neighbor.colorId == startBubble.colorId &&
                    !visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        if (group.Count >= minMatchSize)
        {
            foreach (Bubble b in group)
            {
                allBubbles.Remove(b);
                Destroy(b.gameObject);
            }

            if (scoreManager != null)
                scoreManager.AddScore(group.Count * pointsPerBubble);

            DropDisconnectedBubbles();
            if (allBubbles.Count == 0) Invoke(nameof(GenerateInitialGrid), 0.5f);
        }
    }

    void DropDisconnectedBubbles()
    {
        // 1. Identificar las burbujas que están en el "techo" (Fila 0 o superior)
        // Asumimos que el techo está en la posición Y más alta inicial.
        // Ajusta 'maxY' según tu lógica de generación, aquí uso la posición de la primera fila.
        float maxY = (rows - 1) * 0.5f * bubbleSpacing + gridRoot.position.y; 
        
        HashSet<Bubble> connectedBubbles = new HashSet<Bubble>();
        Queue<Bubble> queue = new Queue<Bubble>();

        // Buscar "Anclas" (Burbujas tocando el techo)
        foreach (Bubble b in allBubbles)
        {
            // Tolerancia de 0.1f para detectar si está en la fila superior
            if (Mathf.Abs(b.transform.position.y - maxY) < 0.1f) 
            {
                connectedBubbles.Add(b);
                queue.Enqueue(b);
            }
        }

        // 2. BFS: Propagar la conexión desde el techo hacia abajo
        while (queue.Count > 0)
        {
            Bubble current = queue.Dequeue();
            
            Collider[] neighbors = Physics.OverlapSphere(current.transform.position, neighborRadius, bubbleLayer);
            foreach (Collider hit in neighbors)
            {
                Bubble neighbor = hit.GetComponent<Bubble>();
                if (neighbor != null && !connectedBubbles.Contains(neighbor) && allBubbles.Contains(neighbor))
                {
                    connectedBubbles.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        // 3. Identificar flotantes (Las que están en allBubbles pero NO en connectedBubbles)
        List<Bubble> floatingBubbles = new List<Bubble>();
        foreach (Bubble b in allBubbles)
        {
            if (!connectedBubbles.Contains(b))
            {
                floatingBubbles.Add(b);
            }
        }

        // 4. Hacer caer las flotantes
        foreach (Bubble b in floatingBubbles)
        {
            allBubbles.Remove(b); // Sacar de la lista lógica
            
            if (b != null)
            {
                Rigidbody rb = b.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false; // Reactivar físicas
                    rb.useGravity = true;   // Que caiga
                    
                    // Empuje aleatorio pequeño para que se vea natural
                    rb.AddForce(new Vector3(Random.Range(-1f, 1f), 0, 0), ForceMode.Impulse);
                }
                // Destruir después de 2 segundos
                Destroy(b.gameObject, 2.0f);
            }
        }
    }
}
