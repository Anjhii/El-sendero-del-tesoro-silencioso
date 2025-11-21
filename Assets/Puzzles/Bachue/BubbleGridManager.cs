/* using System.Collections.Generic;
using UnityEngine;

public class BubbleGridManager : MonoBehaviour
{
    [Header("Grid generation")]
    public Transform gridRoot; // arrastrar BubbleGrid
    public GameObject bubblePrefab;
    public int rows = 6;
    public int columns = 8;
    public float bubbleSpacing = 0.18f;
    public float gridDistance = 5.0f; // distancia desde la cámara
    public LayerMask bubbleLayer;

    [Header("Matching")]
    public float snapDistance = 0.1f; // distancia para considerar vecino / snap
    public float neighborRadius = 0.25f; // overlap para buscar vecinos
    public int minMatchSize = 3;
    public int pointsPerBubble = 10;

    [Header("References")]
    public ScoreManagerBubble scoreManager;

    // estado
    List<Bubble> allBubbles = new List<Bubble>();

    private void Awake()
    {
        if (gridRoot == null) gridRoot = this.transform;
    }

    private void Start()
    {
        // generar grilla inicial
        GenerateInitialGrid();
    }

    public void GenerateInitialGrid()
    {
        if (bubblePrefab == null) return;

        // limpiar hijos previos
        for (int i = gridRoot.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(gridRoot.GetChild(i).gameObject);
        }
        allBubbles.Clear();

        // posición base centrada en X
        Vector3 origin = gridRoot.position;
        float startX = -(columns - 1) * 0.5f * bubbleSpacing;
        float startY = (rows - 1) * 0.5f * bubbleSpacing;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                // alternar offset para un layout compacto si quieres (tipo hex)
                float offsetX = (r % 2 == 0) ? 0f : bubbleSpacing * 0.5f;
                Vector3 pos = origin + new Vector3(startX + c * bubbleSpacing + offsetX, startY - r * bubbleSpacing, 0f);
                GameObject go = Instantiate(bubblePrefab, pos, Quaternion.identity, gridRoot);
                Bubble b = go.GetComponent<Bubble>();
                if (b != null)
                {
                    b.gridManager = this;
                    // si el prefab tiene materiales asignados, deja el color asignado; si no, fuerza aleatorio:
                    if (b.colorMaterials != null && b.colorMaterials.Length > 0)
                        b.SetRandomColor();
                }

                // asegurar layer/tag
                go.layer = Mathf.Clamp(LayerMaskToLayerIndex(bubbleLayer), 0, 31);
                go.tag = "Bubble";

                // marcar rigidbody kinematic para las de la grilla
                Rigidbody rb = go.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                }

                allBubbles.Add(b);
            }
        }
    }

    // helper: convertir layerMask de única capa a índice (asume single layer mask)
    int LayerMaskToLayerIndex(LayerMask mask)
    {
        int m = mask.value;
        for (int i = 0; i < 32; i++)
        {
            if ((m & (1 << i)) != 0) return i;
        }
        return 0;
    }

    public void RegisterBubble(Bubble newBubble)
    {
        if (newBubble == null) return;
        if (!allBubbles.Contains(newBubble)) allBubbles.Add(newBubble);

        // snap: intentar mover la burbuja a la posición más cercana libre dentro del grid
        SnapToNearest(newBubble);

        // check groups desde esta burbuja
        CheckForGroups(newBubble);
    }

    void SnapToNearest(Bubble bubble)
    {
        Vector3 localPos = gridRoot.InverseTransformPoint(bubble.transform.position);

        float step = bubbleSpacing;
        float snapX = Mathf.Round(localPos.x / step) * step;
        float snapY = Mathf.Round(localPos.y / step) * step;

        Vector3 snappedWorld = gridRoot.TransformPoint(new Vector3(snapX, snapY, 0f));

        bubble.transform.position = snappedWorld;
    }


    void CheckForGroups(Bubble startBubble)
    {
        if (startBubble == null) return;

        List<Bubble> group = new List<Bubble>();
        Queue<Bubble> toCheck = new Queue<Bubble>();
        HashSet<Bubble> visited = new HashSet<Bubble>();

        toCheck.Enqueue(startBubble);
        visited.Add(startBubble);

        while (toCheck.Count > 0)
        {
            Bubble current = toCheck.Dequeue();
            group.Add(current);

            Collider[] hits = Physics.OverlapSphere(current.transform.position, neighborRadius, bubbleLayer);
            foreach (Collider hit in hits)
            {
                Bubble neighbor = hit.GetComponent<Bubble>();
                if (neighbor != null && neighbor.colorId == startBubble.colorId && !visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    toCheck.Enqueue(neighbor);
                }
            }
        }

        if (group.Count >= minMatchSize)
        {
            // destruir grupo
            foreach (Bubble b in group)
            {
                if (b == null) continue;
                allBubbles.Remove(b);
                Destroy(b.gameObject);
            }

            // puntaje
            if (scoreManager != null)
                scoreManager.AddScore(group.Count * pointsPerBubble);

            // opcional: comprobar si el campo está vacío -> regenerar
            if (allBubbles.Count == 0)
            {
                // regenerar pequeña pausa
                Invoke(nameof(GenerateInitialGrid), 0.5f);
            }
        }
    }
}
 */

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

    private void SnapToNearest(Bubble bubble)
    {
        Vector3 localPos = gridRoot.InverseTransformPoint(bubble.transform.position);

        float step = bubbleSpacing;

        float snapX = Mathf.Round(localPos.x / step) * step;
        float snapY = Mathf.Round(localPos.y / step) * step;

        Vector3 snappedWorld = gridRoot.TransformPoint(
            new Vector3(snapX, snapY, 0f)
        );

        bubble.transform.position = snappedWorld;
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

            if (allBubbles.Count == 0)
                Invoke(nameof(GenerateInitialGrid), 0.5f);
        }
    }
}
