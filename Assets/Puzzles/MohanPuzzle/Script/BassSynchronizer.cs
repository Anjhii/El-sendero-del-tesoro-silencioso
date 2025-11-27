using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using System;

public class BassSynchronizer : MonoBehaviour
{
    // Definición de datos de nota
    [System.Serializable]
    public class BassNote
    {
        public double dspTime;
        public int lane;
        public float strength;
    }

    [Header("Fuentes de Audio (Stems)")]
    public AudioSource mainTrackSource;  
    public AudioSource bassSource;       
    public AudioSource guitarSource;     

    [Header("Configuración de Juego")]
    public GameObject notePrefab;
    public Transform[] lanes;
    public float noteLeadTime = 2f;      
    public float tutorialDuration = 5f;

    [Header("Input Actions")]
    public InputActionReference startGameAction;
    public InputActionReference exitAction;

    // Eventos
    public event Action OnAnalysisStart;
    public event Action OnTutorialStart;
    public event Action OnWaitingForStart;
    public event Action<int> OnCountdownUpdate;
    public event Action OnGameStart;
    public event Action<int> OnNotesUpdated;
    public event Action OnGameFinished;

    // Configuración interna
    private float detectionThreshold = 0.3f;
    private float minTimeBetweenNotes = 0.2f;
    
    // Variables de sincronización
    private List<BassNote> upcomingNotes = new List<BassNote>();
    private int currentNoteIndex = 0;
    private bool isPlaying = false;
    private double dspSongStartTime;

    // 1. USO DE AWAKE: Esto garantiza que el audio se detenga antes de que siquiera se pinte el primer frame.
    private void Awake()
    {
        StopAutoPlay(mainTrackSource);
        StopAutoPlay(bassSource);
        StopAutoPlay(guitarSource);
    }

    private void StopAutoPlay(AudioSource source)
    {
        if (source != null)
        {
            source.playOnAwake = false;
            source.Stop(); // Forzamos Stop por si acaso estaba sonando en el editor
        }
    }

    private void Start()
    {
        SetupInput();
        StartCoroutine(LoadSequence());
    }

    private void SetupInput()
    {
        if (startGameAction != null)
        {
            startGameAction.action.Enable();
            startGameAction.action.performed += _ => TryStartGame();
        }
        if (exitAction != null)
        {
            exitAction.action.Enable();
            exitAction.action.performed += _ => TryExitGame();
        }
    }

    private IEnumerator LoadSequence()
    {
        OnAnalysisStart?.Invoke();
        
        // Analizamos sin reproducir
        yield return StartCoroutine(AnalyzeTrackCoroutine(bassSource.clip));

        OnTutorialStart?.Invoke();
        yield return new WaitForSeconds(tutorialDuration);

        OnWaitingForStart?.Invoke();
    }

    private IEnumerator AnalyzeTrackCoroutine(AudioClip clip)
    {
        if (clip == null) yield break;
        upcomingNotes.Clear();
        yield return null; 

        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);
        
        int sampleRate = clip.frequency;
        int channels = clip.channels;
        double lastNoteTime = -minTimeBetweenNotes;
        int notesDetected = 0;
        int windowSize = 1024;

        for (int i = 0; i < samples.Length - windowSize; i += windowSize)
        {
            float maxInWindow = 0f;
            for (int j = 0; j < windowSize; j++)
            {
                float val = Mathf.Abs(samples[i + j]);
                if (val > maxInWindow) maxInWindow = val;
            }

            double currentTime = (double)i / (sampleRate * channels);

            if (maxInWindow > detectionThreshold && (currentTime - lastNoteTime) >= minTimeBetweenNotes)
            {
                BassNote note = new BassNote
                {
                    dspTime = currentTime,
                    strength = maxInWindow,
                    lane = GetLaneFromPattern(notesDetected)
                };
                upcomingNotes.Add(note);
                lastNoteTime = currentTime;
                notesDetected++;
            }
            if (i % (windowSize * 1000) == 0) yield return null;
        }
        Debug.Log($"Análisis completado. Notas: {upcomingNotes.Count}");
    }

    private void TryStartGame()
    {
        if (!isPlaying && upcomingNotes.Count > 0)
        {
            StartCoroutine(StartGameRoutine());
        }
    }

    // 2. CORRUTINA DE INICIO: Aquí está la lógica que pediste.
    private IEnumerator StartGameRoutine()
    {
        // Fase de Conteo: El audio NO suena aquí.
        for (int i = 3; i > 0; i--)
        {
            OnCountdownUpdate?.Invoke(i);
            yield return new WaitForSeconds(1f); // Espera 1 segundo real
        }
        
        OnCountdownUpdate?.Invoke(0); 

        // Fase de Juego: El audio arranca SOLO cuando el bucle anterior termina.
        StartSyncedPlayback();
    }

    private void StartSyncedPlayback()
    {
        isPlaying = true;
        currentNoteIndex = 0;
        OnGameStart?.Invoke();
        OnNotesUpdated?.Invoke(upcomingNotes.Count);

        // Agregamos un pequeñísimo delay (0.1s) para asegurar que el motor de audio esté listo tras el conteo
        double scheduledTime = AudioSettings.dspTime + 0.1;
        dspSongStartTime = scheduledTime;

        if(mainTrackSource) mainTrackSource.PlayScheduled(scheduledTime);
        if(bassSource) bassSource.PlayScheduled(scheduledTime);
        if(guitarSource) guitarSource.PlayScheduled(scheduledTime);
    }

    private void Update()
    {
        if (!isPlaying) return;

        double currentSongTime = AudioSettings.dspTime - dspSongStartTime;

        if ((bassSource != null && !bassSource.isPlaying && currentSongTime > bassSource.clip.length + 1) || 
            (currentNoteIndex >= upcomingNotes.Count && currentSongTime > upcomingNotes[upcomingNotes.Count-1].dspTime + 2.0))
        {
            EndGame();
            return;
        }

        while (currentNoteIndex < upcomingNotes.Count)
        {
            BassNote nextNote = upcomingNotes[currentNoteIndex];
            
            if (nextNote.dspTime <= currentSongTime + noteLeadTime)
            {
                SpawnNote(nextNote);
                currentNoteIndex++;
                OnNotesUpdated?.Invoke(upcomingNotes.Count - currentNoteIndex);
            }
            else
            {
                break;
            }
        }
    }

    private void SpawnNote(BassNote noteData)
    {
        if (noteData.lane >= 0 && noteData.lane < lanes.Length)
        {
            Instantiate(notePrefab, lanes[noteData.lane].position, Quaternion.identity);
        }
    }

    private void EndGame()
    {
        isPlaying = false;
        OnGameFinished?.Invoke();
    }

    private void TryExitGame()
    {
        Debug.Log("Saliendo...");
    }

    private int GetLaneFromPattern(int index)
    {
        int[] pattern = { 0, 1, 2, 3, 0, 2, 1, 3 };
        return pattern[index % pattern.Length];
    }

    private void OnDestroy()
    {
        if (startGameAction != null) startGameAction.action.performed -= _ => TryStartGame();
        if (exitAction != null) exitAction.action.performed -= _ => TryExitGame();
    }
}