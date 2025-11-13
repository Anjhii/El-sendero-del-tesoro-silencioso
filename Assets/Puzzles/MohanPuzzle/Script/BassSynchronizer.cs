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
    public float startDelay = 3f;

    [Header("UI Elements - TMPro")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI notesRemainingText;
    public TextMeshProUGUI volumeText;
    public TextMeshProUGUI volumePercentageText; // Nuevo: porcentaje de volumen
    public Slider volumeSlider;
    public Button startStopButton;
    public TextMeshProUGUI startStopButtonText;
    public TextMeshProUGUI controlsText;
    public TextMeshProUGUI totalNotesText; // Nuevo: notas totales

    [Header("Input Actions")]
    public InputActionReference startStopAction;
    public InputActionReference volumeUpAction;
    public InputActionReference volumeDownAction;
    public InputActionReference navigateAction;
    
    // Configuración interna
    private float detectionThreshold = 0.3f;
    private float minTimeBetweenNotes = 0.2f;
    private bool useLanePattern = true;
    
    private List<BassNote> bassNotes = new List<BassNote>();
    private List<BassNote> upcomingNotes = new List<BassNote>();
    private int currentNoteIndex = 0;
    private bool analysisComplete = false;
    private bool gameStarted = false;
    private bool uiSelectionMode = false;
    private int totalNotes = 0;

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

        // Configurar música principal si existe
        if (mainMusicSource != null)
        {
            mainMusicSource.playOnAwake = false;
            mainMusicSource.volume = mainMusicVolume;
        }

        // Configurar Input System
        SetupInputSystem();

        // Configurar UI
        SetupUI();

        // Análisis automático interno
        if (bassTrack != null)
        {
            AnalyzeBassTrack();
        }
        else
        {
            UpdateStatus("❌ No hay pista de bajo");
        }
    }

    void SetupInputSystem()
    {
        // Configurar acciones de input
        if (startStopAction != null)
        {
            startStopAction.action.Enable();
            startStopAction.action.performed += OnStartStopPerformed;
        }

        if (volumeUpAction != null)
        {
            volumeUpAction.action.Enable();
            volumeUpAction.action.performed += OnVolumeUpPerformed;
        }

        if (volumeDownAction != null)
        {
            volumeDownAction.action.Enable();
            volumeDownAction.action.performed += OnVolumeDownPerformed;
        }

        if (navigateAction != null)
        {
            navigateAction.action.Enable();
            navigateAction.action.performed += OnNavigatePerformed;
        }
    }

    void SetupUI()
    {
        // Configurar slider de volumen
        if (volumeSlider != null && mainMusicSource != null)
        {
            volumeSlider.value = mainMusicSource.volume;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        // Configurar botón de inicio/parada
        if (startStopButton != null)
        {
            startStopButton.onClick.AddListener(OnStartStopClicked);
        }

        // Actualizar UI inicial
        UpdateUI();
    }

    void OnEnable()
    {
        // Asegurar que las acciones estén habilitadas
        startStopAction?.action.Enable();
        volumeUpAction?.action.Enable();
        volumeDownAction?.action.Enable();
        navigateAction?.action.Enable();
    }

    void OnDisable()
    {
        // Deshabilitar acciones cuando el objeto se desactive
        startStopAction?.action.Disable();
        volumeUpAction?.action.Disable();
        volumeDownAction?.action.Disable();
        navigateAction?.action.Disable();
    }

    // ========== INPUT HANDLERS ==========

    void OnStartStopPerformed(InputAction.CallbackContext context)
    {
        if (!gameStarted && analysisComplete)
        {
            StartGame();
        }
        else if (gameStarted)
        {
            StopGame();
        }
        UpdateUI();
    }

    void OnVolumeUpPerformed(InputAction.CallbackContext context)
    {
        if (mainMusicSource != null)
        {
            mainMusicSource.volume = Mathf.Min(1, mainMusicSource.volume + 0.1f);
            if (volumeSlider != null) volumeSlider.value = mainMusicSource.volume;
            UpdateUI();
        }
    }

    void OnVolumeDownPerformed(InputAction.CallbackContext context)
    {
        if (mainMusicSource != null)
        {
            mainMusicSource.volume = Mathf.Max(0, mainMusicSource.volume - 0.1f);
            if (volumeSlider != null) volumeSlider.value = mainMusicSource.volume;
            UpdateUI();
        }
    }

    void OnNavigatePerformed(InputAction.CallbackContext context)
    {
        if (!uiSelectionMode && startStopButton != null)
        {
            // Activar modo selección UI
            uiSelectionMode = true;
            startStopButton.Select();
            UpdateControlsText();
        }
    }

    // ========== GAME LOGIC ==========

    void AnalyzeBassTrack()
    {
        if (bassTrack == null) return;

        UpdateStatus("🔍 Analizando pista de bajo...");
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
        
        UpdateStatus($"Listo - {notesDetected} notas detectadas");
        UpdateUI();
    }

    int GetLaneFromPattern(int noteIndex)
    {
        int[] pattern = { 0, 1, 2, 3, 0, 2, 1, 3 };
        return pattern[noteIndex % pattern.Length];
    }

    void StartGame()
    {
        if (!analysisComplete || bassNotes.Count == 0) return;

        StartCoroutine(GameCoroutine());
    }

    IEnumerator GameCoroutine()
    {
        UpdateStatus("⏳ Iniciando en " + startDelay + " segundos...");
        yield return new WaitForSeconds(startDelay);
        
        // Reproducir AMBAS pistas simultáneamente
        if (mainMusicSource != null)
        {
            mainMusicSource.Play();
        }
        
        bassSource.Play();
        currentNoteIndex = 0;
        gameStarted = true;
        uiSelectionMode = false;
        
        UpdateStatus("🎸 ¡Juego en curso!");
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
                UpdateUI();
            }
            
            yield return null;
        }
        
        UpdateStatus("🏁 Juego terminado!");
        gameStarted = false;
        UpdateUI();
    }

    void SpawnNote(int laneIndex)
    {
        if (laneIndex >= 0 && laneIndex < lanes.Length && lanes[laneIndex] != null && notePrefab != null)
        {
            Instantiate(notePrefab, lanes[laneIndex].position, Quaternion.identity);
        }
    }

    void StopGame()
    {
        if (mainMusicSource != null) mainMusicSource.Stop();
        if (bassSource != null) bassSource.Stop();
        gameStarted = false;
        UpdateStatus("⏹️ Juego parado");
        UpdateUI();
    }

    // ========== UI METHODS ==========

    void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log(message);
    }

    void UpdateUI()
    {
        // Actualizar texto de notas restantes
        if (notesRemainingText != null)
        {
            if (gameStarted)
            {
                int remaining = Mathf.Max(0, upcomingNotes.Count - currentNoteIndex);
                notesRemainingText.text = $"Notas Restantes: {remaining}";
            }
            else
            {
                notesRemainingText.text = analysisComplete ? "Listo para jugar" : "Analizando...";
            }
        }

        // Actualizar texto de notas totales
        if (totalNotesText != null)
        {
            totalNotesText.text = $"Notas Totales: {totalNotes}";
        }

        // Actualizar texto de volumen y porcentaje
        if (mainMusicSource != null)
        {
            int volumePercent = Mathf.RoundToInt(mainMusicSource.volume * 100);
            
            if (volumeText != null)
            {
                volumeText.text = $"Volumen: {volumePercent}%";
            }
            
            if (volumePercentageText != null)
            {
                volumePercentageText.text = $"{volumePercent}%";
            }

            // Actualizar slider si existe
            if (volumeSlider != null && Mathf.Abs(volumeSlider.value - mainMusicSource.volume) > 0.01f)
            {
                volumeSlider.value = mainMusicSource.volume;
            }
        }

        // Actualizar botón de inicio/parada
        if (startStopButton != null && startStopButtonText != null)
        {
            if (gameStarted)
            {
                startStopButtonText.text = "PARAR JUEGO";
                startStopButton.interactable = true;
            }
            else
            {
                startStopButtonText.text = "INICIAR JUEGO";
                startStopButton.interactable = analysisComplete;
            }
        }

        UpdateControlsText();
    }

    void UpdateControlsText()
    {
        if (controlsText != null)
        {
            if (uiSelectionMode)
            {
                controlsText.text = "CONTROLES MANDO:\n• A: Confirmar\n• B: Cancelar";
            }
            else
            {
                controlsText.text = "CONTROLES MANDO:\n• A: Iniciar/Parar\n• RB/LB/RT/LT: Destruir notas";
            }
        }
    }

    void OnVolumeChanged(float volume)
    {
        if (mainMusicSource != null)
        {
            mainMusicSource.volume = volume;
            UpdateUI();
        }
    }

    void OnStartStopClicked()
    {
        if (!gameStarted && analysisComplete)
        {
            StartGame();
        }
        else if (gameStarted)
        {
            StopGame();
        }
        UpdateUI();
    }

    // Método público para actualizar desde otros scripts si es necesario
    public void UpdateVolumeDisplay()
    {
        UpdateUI();
    }
}