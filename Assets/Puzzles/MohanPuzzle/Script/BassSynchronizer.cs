using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class BassSynchronizer : MonoBehaviour
{
    [System.Serializable]
    public class BassNote
    {
        public float time;
        public int lane;
        public float strength;
    }

    [Header("Configuración Audio Principal")]
    public AudioSource mainMusicSource;
    public float mainMusicVolume = 0.3f;

    [Header("Configuración Bajo")]
    public AudioClip bassTrack;
    private AudioSource bassSource;
    public float bassVolume = 1.0f;

    [Header("Game Objects")]
    public GameObject notePrefab;
    public Transform[] lanes;
    
    [Header("Ajustes Tiempo")]
    public float noteLeadTime = 2f;
    public float tutorialDuration = 5f;

    [Header("UI Elements - Tutorial")]
    public GameObject tutorialPanel; // Panel con imagen de controles
    public TextMeshProUGUI exitTutorialText; // Texto "Y para salir"
    
    [Header("UI Elements - Pre-Game")]
    public TextMeshProUGUI pressAText; // "Presione A para jugar"
    public TextMeshProUGUI countdownText; // Contador 3, 2, 1

    [Header("UI Elements - In-Game")]
    public TextMeshProUGUI notesRemainingText; // Notas restantes durante el juego

    [Header("UI Elements - End Game")]
    public GameObject endGamePanel; // Panel mensaje final
    public TextMeshProUGUI endGameText; // "Has obtenido un fragmento de una balsa"

    [Header("Input Actions")]
    public InputActionReference startGameAction; // Botón A
    public InputActionReference exitAction; // Botón Y
    public InputActionReference destroyNotesAction; // RB/LB/RT/LT
    
    // Configuración interna
    private float detectionThreshold = 0.3f;
    private float minTimeBetweenNotes = 0.2f;
    private bool useLanePattern = true;
    
    private List<BassNote> bassNotes = new List<BassNote>();
    private List<BassNote> upcomingNotes = new List<BassNote>();
    private int currentNoteIndex = 0;
    private bool analysisComplete = false;
    private bool tutorialComplete = false;
    private bool gameStarted = false;
    private int totalNotes = 0;

    // Estados del juego
    private enum GameState
    {
        Loading,
        Tutorial,
        WaitingForStart,
        Countdown,
        Playing,
        Finished
    }
    private GameState currentState = GameState.Loading;

    void Start()
    {
        // Configurar AudioSource para el bajo
        bassSource = GetComponent<AudioSource>();
        if (bassSource == null)
        {
            bassSource = gameObject.AddComponent<AudioSource>();
        }

        bassSource.clip = bassTrack;
        bassSource.playOnAwake = false;
        bassSource.volume = bassVolume;

        // Configurar música principal
        if (mainMusicSource != null)
        {
            mainMusicSource.playOnAwake = false;
            mainMusicSource.volume = mainMusicVolume;
        }

        // Configurar Input System
        SetupInputSystem();

        // Inicializar UI
        InitializeUI();

        // Análisis automático y mostrar tutorial
        if (bassTrack != null)
        {
            StartCoroutine(LoadAndShowTutorial());
        }
        else
        {
            Debug.LogError("No hay pista de bajo asignada");
        }
    }

    void SetupInputSystem()
    {
        if (startGameAction != null)
        {
            startGameAction.action.Enable();
            startGameAction.action.performed += OnStartGamePressed;
        }

        if (exitAction != null)
        {
            exitAction.action.Enable();
            exitAction.action.performed += OnExitPressed;
        }

        if (destroyNotesAction != null)
        {
            destroyNotesAction.action.Enable();
        }
    }

    void InitializeUI()
    {
        // Ocultar todos los elementos UI al inicio
        SetUIVisibility(tutorialPanel, false);
        SetTextVisibility(exitTutorialText, false);
        SetTextVisibility(pressAText, false);
        SetTextVisibility(countdownText, false);
        SetTextVisibility(notesRemainingText, false);
        SetUIVisibility(endGamePanel, false);
    }

    // ========== COROUTINES DE FLUJO DEL JUEGO ==========

    IEnumerator LoadAndShowTutorial()
    {
        currentState = GameState.Loading;
        
        // Analizar pista de bajo
        AnalyzeBassTrack();
        
        // Esperar a que el análisis complete
        while (!analysisComplete)
        {
            yield return null;
        }

        // Mostrar tutorial
        currentState = GameState.Tutorial;
        ShowTutorial();
        
        // Esperar 5 segundos
        yield return new WaitForSeconds(tutorialDuration);
        
        // Pasar a esperar inicio
        tutorialComplete = true;
        currentState = GameState.WaitingForStart;
        ShowWaitingForStart();
    }

    void ShowTutorial()
    {
        SetUIVisibility(tutorialPanel, true);
        SetTextVisibility(exitTutorialText, true);
        
        if (exitTutorialText != null)
        {
            exitTutorialText.text = "Y para salir";
        }
    }

    void ShowWaitingForStart()
    {
        // Ocultar tutorial
        SetUIVisibility(tutorialPanel, false);
        SetTextVisibility(exitTutorialText, false);
        
        // Mostrar mensaje de inicio
        SetTextVisibility(pressAText, true);
        if (pressAText != null)
        {
            pressAText.text = "Presione A para jugar";
        }
    }

    IEnumerator StartGameSequence()
    {
        currentState = GameState.Countdown;
        
        // Ocultar mensaje de inicio
        SetTextVisibility(pressAText, false);
        
        // Mostrar countdown
        SetTextVisibility(countdownText, true);
        
        for (int i = 3; i > 0; i--)
        {
            if (countdownText != null)
            {
                countdownText.text = i.ToString();
            }
            yield return new WaitForSeconds(1f);
        }
        
        // Ocultar countdown
        SetTextVisibility(countdownText, false);
        
        // Iniciar juego
        currentState = GameState.Playing;
        StartGame();
    }

    void StartGame()
    {
        // Mostrar notas restantes
        SetTextVisibility(notesRemainingText, true);
        UpdateNotesRemaining();
        
        // Reproducir música
        if (mainMusicSource != null)
        {
            mainMusicSource.Play();
        }
        
        bassSource.Play();
        currentNoteIndex = 0;
        gameStarted = true;
        
        StartCoroutine(SpawnNotesCoroutine());
    }

    IEnumerator SpawnNotesCoroutine()
    {
        while (currentNoteIndex < upcomingNotes.Count && bassSource.isPlaying)
        {
            BassNote nextNote = upcomingNotes[currentNoteIndex];
            float timeUntilNote = nextNote.time - bassSource.time;
            
            if (timeUntilNote <= noteLeadTime && timeUntilNote >= -0.1f)
            {
                SpawnNote(nextNote.lane);
                currentNoteIndex++;
                UpdateNotesRemaining();
                
                // Verificar si terminó el juego
                if (GetNotesRemaining() == 0)
                {
                    yield return new WaitForSeconds(2f); // Esperar un poco antes de mostrar mensaje final
                    ShowEndGame();
                    yield break;
                }
            }
            
            yield return null;
        }
    }

    void ShowEndGame()
    {
        currentState = GameState.Finished;
        gameStarted = false;
        
        // Ocultar notas restantes
        SetTextVisibility(notesRemainingText, false);
        
        // Mostrar mensaje final
        SetUIVisibility(endGamePanel, true);
        if (endGameText != null)
        {
            endGameText.text = "¡Has obtenido un fragmento de una balsa!";
        }
    }

    // ========== INPUT HANDLERS ==========

    void OnStartGamePressed(InputAction.CallbackContext context)
    {
        if (currentState == GameState.WaitingForStart)
        {
            StartCoroutine(StartGameSequence());
        }
    }

    void OnExitPressed(InputAction.CallbackContext context)
    {
        if (currentState == GameState.Tutorial || currentState == GameState.WaitingForStart)
        {
            // Salir del juego o volver al menú principal
            Debug.Log("Saliendo del nivel...");
            // Aquí puedes agregar tu lógica para salir
            // Por ejemplo: SceneManager.LoadScene("MainMenu");
        }
    }

    // ========== GAME LOGIC ==========

    void AnalyzeBassTrack()
    {
        if (bassTrack == null) return;

        bassNotes.Clear();

        float[] samples = new float[bassTrack.samples * bassTrack.channels];
        bassTrack.GetData(samples, 0);
        
        int sampleRate = bassTrack.frequency;
        int channels = bassTrack.channels;

        float lastNoteTime = -minTimeBetweenNotes;
        int notesDetected = 0;
        int windowSize = Mathf.Min(441, samples.Length / 100);
        
        for (int i = 0; i < samples.Length - windowSize; i += windowSize)
        {
            float maxInWindow = 0f;
            
            for (int j = 0; j < windowSize && i + j < samples.Length; j++)
            {
                float absSample = Mathf.Abs(samples[i + j]);
                if (absSample > maxInWindow) maxInWindow = absSample;
            }
            
            float currentTime = (float)i / (sampleRate * channels);
            
            if (maxInWindow > detectionThreshold && currentTime - lastNoteTime >= minTimeBetweenNotes)
            {
                BassNote note = new BassNote();
                note.time = currentTime;
                note.strength = maxInWindow;
                note.lane = useLanePattern ? GetLaneFromPattern(notesDetected) : Random.Range(0, lanes.Length);
                
                bassNotes.Add(note);
                lastNoteTime = currentTime;
                notesDetected++;
            }
        }
        
        analysisComplete = true;
        totalNotes = notesDetected;
        upcomingNotes = new List<BassNote>(bassNotes);
        upcomingNotes.Sort((a, b) => a.time.CompareTo(b.time));
        
        Debug.Log($"Análisis completo: {notesDetected} notas detectadas");
    }

    int GetLaneFromPattern(int noteIndex)
    {
        int[] pattern = { 0, 1, 2, 3, 0, 2, 1, 3 };
        return pattern[noteIndex % pattern.Length];
    }

    void SpawnNote(int laneIndex)
    {
        if (laneIndex >= 0 && laneIndex < lanes.Length && lanes[laneIndex] != null && notePrefab != null)
        {
            Instantiate(notePrefab, lanes[laneIndex].position, Quaternion.identity);
        }
    }

    // ========== UI HELPERS ==========

    void UpdateNotesRemaining()
    {
        if (notesRemainingText != null && gameStarted)
        {
            int remaining = GetNotesRemaining();
            notesRemainingText.text = $"Notas Restantes: {remaining}";
        }
    }

    int GetNotesRemaining()
    {
        return Mathf.Max(0, upcomingNotes.Count - currentNoteIndex);
    }

    void SetUIVisibility(GameObject uiElement, bool visible)
    {
        if (uiElement != null)
        {
            uiElement.SetActive(visible);
        }
    }

    void SetTextVisibility(TextMeshProUGUI textElement, bool visible)
    {
        if (textElement != null)
        {
            textElement.gameObject.SetActive(visible);
        }
    }

    // ========== CLEANUP ==========

    void OnEnable()
    {
        startGameAction?.action.Enable();
        exitAction?.action.Enable();
        destroyNotesAction?.action.Enable();
    }

    void OnDisable()
    {
        startGameAction?.action.Disable();
        exitAction?.action.Disable();
        destroyNotesAction?.action.Disable();
    }

    void OnDestroy()
    {
        if (startGameAction != null)
            startGameAction.action.performed -= OnStartGamePressed;
        
        if (exitAction != null)
            exitAction.action.performed -= OnExitPressed;
    }
}