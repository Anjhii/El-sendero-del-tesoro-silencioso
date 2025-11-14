using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Player Data")]
    public string playerName = "Hunza";
    
    [Header("Managers")]
    public FragmentManager fragmentManager;
    public UIManager uiManager;
    
    public static GameManager Instance;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // Cargar nombre de usuario si existe
            playerName = PlayerPrefs.GetString("Username", "Zipa Hunza");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Inicializar UI con datos actuales
        if (uiManager != null)
            uiManager.ActualizarUI();
    }
    
    /// <summary>
    /// Llamado cuando el jugador entra al templo - inicia el puzzle correspondiente
    /// </summary>
    public void IniciarPuzzle(string nombreTemplo)
    {
        Debug.Log($"Iniciando puzzle del templo: {nombreTemplo}");
        
        // Aquí activas el puzzle específico según el templo
        switch (nombreTemplo)
        {
            case "Mohán":
                IniciarPuzzleMohán();
                break;
                
            case "Madremonte":
                IniciarPuzzleMadremonte();
                break;
                
            case "Bachué":
                IniciarPuzzleBachué();
                break;
                
            default:
                Debug.LogWarning($"Templo desconocido: {nombreTemplo}");
                break;
        }
    }

    /// <summary>
    /// Inicia el puzzle específico del Mohán (Guitar Hero)
    /// </summary>
    void IniciarPuzzleMohán()
    {
        Debug.Log("Iniciando puzzle de la Madremonte");
        StartCoroutine(SimularPuzzleCompletado("Madremonte"));
    }

    void IniciarPuzzleMadremonte()
    {
        Debug.Log("Iniciando puzzle de la Madremonte");
        StartCoroutine(SimularPuzzleCompletado("Madremonte"));
    }

    void IniciarPuzzleBachué()
    {
        Debug.Log("Iniciando puzzle de Bachué");
        StartCoroutine(SimularPuzzleCompletado("Bachué"));
    }

    System.Collections.IEnumerator SimularPuzzleCompletado(string templo)
    {
        // Simular tiempo de puzzle
        yield return new WaitForSeconds(3f);
        
        // Completar con puntaje aleatorio para testing
        int puntaje = Random.Range(300, 950);
        FinalizarPuzzle(templo, puntaje);
    }
    
    /// <summary>
    /// Llamar cuando un puzzle termina
    /// </summary>
    public void FinalizarPuzzle(string nombreTemplo, int puntajeObtenido)
    {
        Debug.Log($"Puzzle {nombreTemplo} completado. Puntaje: {puntajeObtenido}");
        
        // Asignar fragmento usando tu sistema
        fragmentManager.AsignarFragmento(nombreTemplo, puntajeObtenido);
        
        // Actualizar UI
        if (uiManager != null)
            uiManager.ActualizarUI();
        
        // Notificar al movimiento que el puzzle terminó
        /* MovimientoGamepad movimiento = FindObjectOfType<MovimientoGamepad>();
        if (movimiento != null)
        {
            movimiento.PuzzleTerminado();
        } */
        
        // Verificar victoria
        VerificarFinDelJuego();
    }
    
    void VerificarFinDelJuego()
    {
        if (fragmentManager.TodosLosFragmentosCompletados())
        {
            int luminosos = fragmentManager.ContarLuminosos();
            bool finalBueno = luminosos >= 2;
            
            Debug.Log($"Juego completado. Fragmentos luminosos: {luminosos}/3");
            
            // Cargar escena final según resultado
            string escena = finalBueno ? "FinalBueno" : "FinalRegular";
            SceneManager.LoadScene(escena);
        }
    }
    
    /// <summary>
    /// Para testing - Simular completar puzzle
    /// </summary>
    [ContextMenu("Test Puzzle Mohán (Alto Puntaje)")]
    void TestPuzzleAltoPuntaje()
    {
        FinalizarPuzzle("Mohán", 850);
    }
    
    [ContextMenu("Test Puzzle Madremonte (Bajo Puntaje)")]
    void TestPuzzleBajoPuntaje()
    {
        FinalizarPuzzle("Madremonte", 500);
    }
}