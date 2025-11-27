using UnityEngine;
using UnityEngine.Video;
using TMPro;
using System.Collections; // Necesario para IEnumerator

public class ManagerInterfaz : MonoBehaviour
{
    [Header("Referencia al Sync Logic")]
    public BassSynchronizer syncLogic;

    [Header("Configuración Tiempos")]
    public float duracionTutorial = 5.0f; // Tiempo que dura el tutorial visible

    [Header("Video Intro")]
    public VideoPlayer introVideoPlayer;
    public GameObject introPanel;

    [Header("UI Panels")]
    public GameObject loadingPanel;
    public GameObject tutorialPanel;
    public GameObject preGamePanel;
    public GameObject hudPanel;
    public GameObject endGamePanel;

    [Header("Text Elements")]
    public TextMeshProUGUI tutorialExitText;
    public TextMeshProUGUI pressStartText;
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI notesRemainingText;
    public TextMeshProUGUI endGameMessageText;

    private bool introPlayed = false;
    private bool enSecuenciaIntro = false; // Flag para evitar que otros eventos interrumpan

    private void Start()
    {
        HideAllPanels();

        // 1. Mostrar intro primero
        if (introPanel && introVideoPlayer && !introPlayed)
        {
            introPanel.SetActive(true);
            enSecuenciaIntro = true; // Bloqueamos interrupciones
            introVideoPlayer.loopPointReached += OnIntroFinished;
            introVideoPlayer.Play();
        }
        else
        {
            // Si no hay video, saltar directo a la lógica
            InitializeSyncLogic();
            HandleWaitingForStart();
        }
    }

    // 2. Al terminar el video
    private void OnIntroFinished(VideoPlayer vp)
    {
        introPlayed = true;
        
        // Desuscribir para evitar errores si se reproduce de nuevo (opcional)
        vp.loopPointReached -= OnIntroFinished; 
        
        if (introPanel) introPanel.SetActive(false);

        // Inicializamos la lógica del juego (para que cargue datos/audio de fondo si es necesario)
        InitializeSyncLogic();

        // Iniciamos la secuencia temporal del tutorial
        StartCoroutine(SecuenciaTutorialEInicio());
    }

    // 3. Secuencia: Tutorial (5s) -> Panel Inicio
    private IEnumerator SecuenciaTutorialEInicio()
    {
        // Forzamos mostrar el tutorial
        HideAllPanels();
        if (tutorialPanel) tutorialPanel.SetActive(true);
        if (tutorialExitText) tutorialExitText.text = "Prepárate...";

        // Esperamos 5 segundos
        yield return new WaitForSeconds(duracionTutorial);

        // Terminó la secuencia de intro, ahora sí mostramos el "Presione A"
        enSecuenciaIntro = false; 
        
        // Llamamos manualmente al estado de espera
        HandleWaitingForStart();
    }

    private void InitializeSyncLogic()
    {
        if (syncLogic != null)
        {
            // Suscribimos los eventos
            syncLogic.OnAnalysisStart += HandleAnalysisStart;
            // syncLogic.OnTutorialStart += HandleTutorialStart; // COMENTADO: Lo manejamos manualmente en la intro
            syncLogic.OnWaitingForStart += HandleWaitingForStart;
            syncLogic.OnCountdownUpdate += HandleCountdown;
            syncLogic.OnGameStart += HandleGameStart;
            syncLogic.OnNotesUpdated += UpdateNotesCounter;
            syncLogic.OnGameFinished += HandleGameFinished;
        }
        else
        {
            Debug.LogError("GameUI: No se ha asignado InstrumentSync.");
        }
    }

    // --- Manejadores de Eventos ---

    private void HandleAnalysisStart()
    {
        // Solo mostramos carga si NO estamos en la secuencia de intro/tutorial
        if (!enSecuenciaIntro)
        {
            HideAllPanels();
            if (loadingPanel) loadingPanel.SetActive(true);
        }
    }

    private void HandleWaitingForStart()
    {
        // 4. Panel de Inicio
        // Solo permitimos mostrar este panel si la secuencia de tutorial ya terminó
        if (!enSecuenciaIntro)
        {
            HideAllPanels();
            if (preGamePanel) preGamePanel.SetActive(true);
            if (pressStartText) pressStartText.text = "Presione A para jugar";
        }
    }

    private void HandleCountdown(int count)
    {
        // 5. Timer (Conteo regresivo)
        if (preGamePanel) preGamePanel.SetActive(false);

        if (count > 0)
        {
            if (countdownText)
            {
                countdownText.gameObject.SetActive(true);
                countdownText.text = count.ToString();
            }
        }
        else
        {
            if (countdownText) countdownText.gameObject.SetActive(false);
        }
    }

    private void HandleGameStart()
    {
        // 6. Inicio del juego (Se activa cuando termina el countdown)
        HideAllPanels();
        if (hudPanel) hudPanel.SetActive(true);
    }

    private void UpdateNotesCounter(int remaining)
    {
        // 7. Actualización de notas
        if (notesRemainingText != null)
        {
            if (!notesRemainingText.gameObject.activeSelf)
                notesRemainingText.gameObject.SetActive(true);

            notesRemainingText.text = $"Notas: {remaining}";
        }
    }

    private void HandleGameFinished()
    {
        // 8. Panel Final
        HideAllPanels();
        if (endGamePanel) endGamePanel.SetActive(true);
        if (endGameMessageText) endGameMessageText.text = "¡Fragmento obtenido!";
    }

    private void HideAllPanels()
    {
        if (loadingPanel) loadingPanel.SetActive(false);
        if (tutorialPanel) tutorialPanel.SetActive(false);
        if (preGamePanel) preGamePanel.SetActive(false);
        if (hudPanel) hudPanel.SetActive(false);
        if (endGamePanel) endGamePanel.SetActive(false);
        if (countdownText) countdownText.gameObject.SetActive(false);
    }
}