using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PuzzleManager : MonoBehaviour
{
    [Header("Paneles UI")]
    public GameObject instructionsPanel; // El panel con Imagen + Texto de controles
    public GameObject gameHUDPanel;      // El panel con "# Restantes"

    [Header("Referencias HUD")]
    public TextMeshProUGUI bubblesRemainingText; // El texto TMP de # restantes
    
    [Header("Tiempos")]
    public float startDelay = 1.0f;      // Esperar antes de mostrar instrucciones
    public float showDuration = 11.0f;   // Cuánto tiempo mostrar instrucciones

    private BubbleGridManager gridManager;

    void Start()
    {
        // 1. Estado Inicial: Todo apagado (o HUD apagado si prefieres)
        if(instructionsPanel != null) instructionsPanel.SetActive(false);
        if(gameHUDPanel != null) gameHUDPanel.SetActive(true); // HUD puede estar activo siempre si quieres

        // Referencia al manager para contar bolas (opcional si lo haces por evento)
        gridManager = FindObjectOfType<BubbleGridManager>();

        // 2. Iniciar la secuencia
        StartCoroutine(IntroSequence());
    }

    void Update()
    {
        // Actualizar el texto de burbujas restantes constantemente
        if (gridManager != null && bubblesRemainingText != null)
        {
            // Asumiendo que 'allBubbles' es público o tienes un método GetBubbleCount()
            // Si 'allBubbles' es privado en BubbleGridManager, tendrás que hacerlo público o crear un getter.
            // Por ahora uso un ejemplo genérico:
             bubblesRemainingText.text = $"Restantes: {gridManager.GetBubbleCount()}";
        }
    }

    IEnumerator IntroSequence()
    {
        // Espera inicial
        yield return new WaitForSeconds(startDelay);

        // Mostrar instrucciones
        if(instructionsPanel != null) instructionsPanel.SetActive(true);

        // Esperar duración
        yield return new WaitForSeconds(showDuration);

        // Ocultar instrucciones
        if(instructionsPanel != null) instructionsPanel.SetActive(false);
        
        // Asegurar que el HUD de juego esté visible
        if(gameHUDPanel != null) gameHUDPanel.SetActive(true);
    }
}
