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

    [Header("VFX")]
    public GameObject popEffectPrefab; // Arrastra aquí tu prefab 'BubblePop'

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

    public void RegisterBubble(Bubble bubble, Vector3 impactPoint)
    {
        SnapToNearest(bubble, impactPoint);
        
        // ... resto de tu lógica de añadir a la lista y buscar grupos ...
        if (!allBubbles.Contains(bubble)) allBubbles.Add(bubble);
        bubble.transform.localRotation = Quaternion.identity;
        CheckForGroups(bubble);
    }

    void SnapToNearest(Bubble bubble, Vector3 impactPoint)
    {
        // Convertir punto de impacto a local
        Vector3 localImpact = gridRoot.InverseTransformPoint(impactPoint);

        // Encontrar la coordenada hexagonal teórica más cercana al impacto
        // Pero retrocediendo un poco hacia de donde vino la bola para no meterse DENTRO de la otra
        // (Usamos la posición de la bola antes del snap como referencia de dirección)
        Vector3 localBubblePos = gridRoot.InverseTransformPoint(bubble.transform.position);
        Vector3 dir = (localBubblePos - localImpact).normalized;
        
        // "Retrocedemos" medio radio desde el impacto hacia afuera para encontrar el centro de celda ideal
        Vector3 idealCenter = localImpact + (dir * bubbleSpacing * 0.5f);

        float bestDist = float.MaxValue;
        Vector3 finalPos = bubble.transform.position;

        // Buscar en un radio pequeño alrededor del punto ideal cuál celda de la grilla está VACÍA
        // Generamos candidatos alrededor
        List<Vector3> candidates = GetHexNeighborPositions(idealCenter);
        candidates.Add(GetHexPosition(idealCenter)); // Añadir la propia posición calculada

        foreach (Vector3 candidate in candidates)
        {
            // Verificar si esta posición candidata ya está ocupada por OTRA bola
            if (!IsOccupied(candidate, bubble))
            {
                float d = Vector3.Distance(idealCenter, candidate);
                if (d < bestDist)
                {
                    bestDist = d;
                    finalPos = candidate;
                }
            }
        }

        bubble.transform.localPosition = finalPos;
    }

    // Verifica si hay una bola en esa coordenada (con margen de error)
    bool IsOccupied(Vector3 localPos, Bubble self)
{
    // Recorremos hacia atrás para poder eliminar elementos de la lista si es necesario
    for (int i = allBubbles.Count - 1; i >= 0; i--)
    {
        Bubble b = allBubbles[i];

        // CORRECCIÓN: Si la bola es null (fue destruida), la sacamos de la lista y continuamos
        if (b == null || b.gameObject == null)
        {
            allBubbles.RemoveAt(i);
            continue;
        }

        if (b == self) continue;

        // Comprobación de distancia
        if (Vector3.Distance(b.transform.localPosition, localPos) < bubbleSpacing * 0.5f)
        {
            return true; 
        }
    }
    return false;
}

    // Convierte una posición arbitraria al centro de celda hexagonal más cercano
    Vector3 GetHexPosition(Vector3 localPos)
    {
        float q = localPos.x / bubbleSpacing; // Columna aprox
        float r = localPos.y / (bubbleSpacing * 0.866f); // Fila aprox (sin2 60 = 0.866)

        int gridY = Mathf.RoundToInt(r);
        // Si la fila es impar, desplazamos X por 0.5
        float offsetX = (Mathf.Abs(gridY) % 2 == 1) ? 0.5f : 0.0f;
        int gridX = Mathf.RoundToInt(q - offsetX);

        float finalX = (gridX + offsetX) * bubbleSpacing;
        float finalY = gridY * (bubbleSpacing * 0.866f);

        return new Vector3(finalX, finalY, 0);
    }

    List<Vector3> GetHexNeighborPositions(Vector3 centerLocal)
    {
        List<Vector3> neighbors = new List<Vector3>();
        // Direcciones hexagonales relativas
        Vector3[] offsets = {
            new Vector3(1, 0, 0), new Vector3(-1, 0, 0),
            new Vector3(0.5f, 0.866f, 0), new Vector3(-0.5f, 0.866f, 0),
            new Vector3(0.5f, -0.866f, 0), new Vector3(-0.5f, -0.866f, 0)
        };

        foreach(var off in offsets)
        {
            // Multiplicamos por spacing pero recalculamos el Grid Snap para precisión
            neighbors.Add(GetHexPosition(centerLocal + off * bubbleSpacing)); 
        }
        return neighbors;
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
        // 1. Buscar coincidencias (Flood Fill)
        List<Bubble> group = new List<Bubble>();
        Queue<Bubble> queue = new Queue<Bubble>();
        HashSet<Bubble> visited = new HashSet<Bubble>();

        queue.Enqueue(startBubble);
        visited.Add(startBubble);
        
        // Necesitamos el ID de color para comparar
        int targetColorId = startBubble.colorId;

        while (queue.Count > 0)
        {
            Bubble current = queue.Dequeue();
            group.Add(current);

            Collider[] hits = Physics.OverlapSphere(current.transform.position, neighborRadius, bubbleLayer);

            foreach (Collider hit in hits)
            {
                Bubble neighbor = hit.GetComponent<Bubble>();
                // Verificar NULL antes de acceder
                if (neighbor != null && neighbor.gameObject != null && !visited.Contains(neighbor))
                {
                    if (neighbor.colorId == targetColorId) // Solo si es del mismo color
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        // 2. Si el grupo es de 3 o más -> DESTRUIR
        if (group.Count >= minMatchSize)
        {
            foreach (Bubble b in group)
            {
                if (b != null)
                {
                    // OBTENER EL COLOR ANTES DE DESTRUIR
                    Color bColor = Color.white;
                    if(b.TryGetComponent(out Renderer r)) bColor = r.material.color;

                    // EFECTO VISUAL
                    PlayPopEffect(b.transform.position, bColor);

                    allBubbles.Remove(b);
                    Destroy(b.gameObject);
                }
            }

            // Sistema de Puntuación (Opcional)
            if (scoreManager != null) scoreManager.AddScore(group.Count * pointsPerBubble);

            // 3. 🔥 ¡AQUÍ SE LLAMA A LA CAÍDA DE ISLAS! 🔥
            // Esto solo se ejecuta si hubo una explosión
            DropDisconnectedBubbles();

            // Regenerar si se limpia todo
            if (allBubbles.Count == 0) Invoke(nameof(GenerateInitialGrid), 1.0f);
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

        // 4. ELIMINAR FLOTANTES CON EXPLOSIÓN
        foreach (Bubble b in floatingBubbles)
        {
            if (b != null)
            {
                // OBTENER COLOR
                Color bColor = Color.white;
                if (b.TryGetComponent(out Renderer r)) bColor = r.material.color;

                // EFECTO VISUAL
                PlayPopEffect(b.transform.position, bColor);

                // LOGICA DE DESTRUCCIÓN
                allBubbles.Remove(b);
                Destroy(b.gameObject); // Destrucción inmediata o con pequeño delay secuencial
                
                // Opcional: Sumar puntos extra por islas caídas
                if(scoreManager != null) scoreManager.AddScore(pointsPerBubble * 2); 
            }
        }
    }


    private void PlayPopEffect(Vector3 position, Color bubbleColor)
    {
        if (popEffectPrefab != null)
        {
            // Instanciar la explosión
            GameObject vfx = Instantiate(popEffectPrefab, position, Quaternion.identity);
            
            // Asignar el color de la bola a las partículas
            var ps = vfx.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = bubbleColor; // Pinta las partículas
            }

            // Destruir el efecto visual después de 1 segundo para limpiar memoria
            Destroy(vfx, 1.0f);
        }
    }
}
