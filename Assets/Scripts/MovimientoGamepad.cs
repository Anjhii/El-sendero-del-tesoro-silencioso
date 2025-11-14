using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public class MovimientoGamepad : MonoBehaviour
{
    [Header("Referencias VR")]
    public Transform cabezaVR;
    public float velocidadMovimiento = 2f;
    public InputActionReference accionMovimiento;
    public InputActionReference accionInteractuar; // Tecla A
    public InputActionReference accionSalirPuzzle; // Tecla Y

    [Header("Configuración Carriles")]
    public CarrilData[] carriles;
    public float fuerzaAtraccion = 8f;
    public float reduccionVelocidad = 0.3f;
    
    [Header("Posiciones de Reaparición")]
    public Transform posicionInicioCarril2;
    public Transform posicionInicioCarril3;
    public Transform posicionFinalCarril3;
    
    private CharacterController controller;
    private Vector2 inputMovimiento;
    private int carrilActualIndex = 0;
    private Transform[] puntosCarrilActual;
    private Vector3 puntoCarrilMasCercano;
    private bool enPuntoFinal = false;
    private float velocidadActual;
    private bool enEscenaPuzzle = false;

    [System.Serializable]
    public class CarrilData
    {
        public string nombre;
        public Transform[] puntosCarril;
        public float anchoCarril = 2f;
        public Transform puntoFinal;
        public string nombreEscenaPuzzle;
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        velocidadActual = velocidadMovimiento;
        
        if (carriles != null && carriles.Length > 0)
        {
            ActivarCarril(0);
        }
    }

    void Update()
    {
        // Si está en escena de puzzle, solo procesar tecla Y para salir
        if (enEscenaPuzzle)
        {
            ProcesarSalidaPuzzle();
            return;
        }

        inputMovimiento = accionMovimiento.action.ReadValue<Vector2>();
        
        if (!enPuntoFinal)
        {
            MoverJugador();
        }
        
        ForzarSuelo();
        AplicarSistemaCarril();
        VerificarPuntoFinal();
        ProcesarInputInteraccion();
    }

    void MoverJugador()
    {
        if (cabezaVR == null) return;

        Vector3 direccionCarril = CalcularDireccionCarril();
        Vector3 direccionLateral = Vector3.Cross(Vector3.up, direccionCarril).normalized;
        
        Vector3 movimiento = (direccionCarril * inputMovimiento.y + direccionLateral * inputMovimiento.x) 
                           * velocidadActual * Time.deltaTime;
        
        controller.Move(movimiento);
    }

    void AplicarSistemaCarril()
    {
        if (puntosCarrilActual == null) return;

        EncontrarPuntoCarrilMasCercano();
        float distanciaAlCarril = Vector3.Distance(
            new Vector3(transform.position.x, 0, transform.position.z),
            new Vector3(puntoCarrilMasCercano.x, 0, puntoCarrilMasCercano.z)
        );

        float anchoPermitido = carriles[carrilActualIndex].anchoCarril * 0.5f;
        
        if (distanciaAlCarril > anchoPermitido * 0.8f)
        {
            float factor = (distanciaAlCarril - anchoPermitido * 0.8f) / (anchoPermitido * 0.2f);
            velocidadActual = Mathf.Lerp(velocidadMovimiento, velocidadMovimiento * reduccionVelocidad, factor);
        }
        else
        {
            velocidadActual = velocidadMovimiento;
        }

        if (distanciaAlCarril > anchoPermitido * 0.6f)
        {
            Vector3 direccionAlCarril = (puntoCarrilMasCercano - transform.position).normalized;
            direccionAlCarril.y = 0;
            
            float fuerza = Mathf.Clamp01((distanciaAlCarril - anchoPermitido * 0.6f) / anchoPermitido) * fuerzaAtraccion;
            controller.Move(direccionAlCarril * fuerza * Time.deltaTime);
        }
    }

    void ForzarSuelo()
    {
        if (!controller.isGrounded)
        {
            controller.Move(Vector3.down * 10f * Time.deltaTime);
        }
    }

    void VerificarPuntoFinal()
    {
        if (carriles[carrilActualIndex].puntoFinal == null) return;

        Transform puntoFinal = carriles[carrilActualIndex].puntoFinal;
        float distanciaAlFinal = Vector3.Distance(transform.position, puntoFinal.position);

        if (distanciaAlFinal < 1.5f && !enPuntoFinal)
        {
            enPuntoFinal = true;
            Debug.Log($"¡Llegaste al final del {carriles[carrilActualIndex].nombre}! Presiona A para interactuar.");
        }
        else if (distanciaAlFinal >= 1.5f && enPuntoFinal)
        {
            enPuntoFinal = false;
        }
    }

    void ProcesarInputInteraccion()
    {
        if (accionInteractuar.action.triggered && enPuntoFinal && !enEscenaPuzzle)
        {
            ManejarInteraccionPuntoFinal();
        }
    }

    void ProcesarSalidaPuzzle()
    {
        if (accionSalirPuzzle.action.triggered && enEscenaPuzzle)
        {
            SalirDelPuzzle();
        }
    }

    void ManejarInteraccionPuntoFinal()
    {
        switch (carrilActualIndex)
        {
            case 0: // Carril 1 - Cargar escena Mohan_puzzle
                CargarEscenaPuzzle("Mohan_Puzzle");
                break;
                
            case 1: // Carril 2 - Cargar escena Bachue_puzzle
                CargarEscenaPuzzle("Bachue_Puzzle");
                break;
                
            case 2: // Carril 3 - Ir a posición final
                CompletarCarril3();
                break;
        }
        
        enPuntoFinal = false;
    }

    void CargarEscenaPuzzle(string nombreEscena)
    {
        if (SceneExists(nombreEscena))
        {
            enEscenaPuzzle = true;
            Debug.Log($"Cargando puzzle: {nombreEscena}");
            SceneManager.LoadScene(nombreEscena);
        }
        else
        {
            Debug.LogError($"Escena de puzzle '{nombreEscena}' no encontrada!");
        }
    }

    void SalirDelPuzzle()
    {
        // Regresar a la escena principal de carriles
        string escenaPrincipal = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(escenaPrincipal);
        
        // Programar la reaparición para el siguiente frame
        StartCoroutine(ReaparecerDespuesDePuzzle());
    }

    System.Collections.IEnumerator ReaparecerDespuesDePuzzle()
    {
        // Esperar un frame para que la escena se cargue completamente
        yield return null;
        
        switch (carrilActualIndex)
        {
            case 0: // Salir de Mohan_puzzle -> Carril 2
                if (posicionInicioCarril2 != null)
                {
                    controller.enabled = false;
                    transform.position = posicionInicioCarril2.position;
                    transform.rotation = posicionInicioCarril2.rotation;
                    controller.enabled = true;
                    ActivarCarril(1);
                    Debug.Log("Reapareciendo en Carril 2");
                }
                break;
                
            case 1: // Salir de Bachue_puzzle -> Carril 3
                if (posicionInicioCarril3 != null)
                {
                    controller.enabled = false;
                    transform.position = posicionInicioCarril3.position;
                    transform.rotation = posicionInicioCarril3.rotation;
                    controller.enabled = true;
                    ActivarCarril(2);
                    Debug.Log("Reapareciendo en Carril 3");
                }
                break;
        }
        
        enEscenaPuzzle = false;
    }

    void CompletarCarril3()
    {
        Debug.Log("¡Has completado todos los carriles!");
        
        if (posicionFinalCarril3 != null)
        {
            controller.enabled = false;
            transform.position = posicionFinalCarril3.position;
            transform.rotation = posicionFinalCarril3.rotation;
            controller.enabled = true;
        }
        
        // Aquí puedes agregar lógica adicional para el final del juego
        // como mostrar una pantalla de victoria, etc.
    }

    // Método para verificar si la escena existe (de tu LoginManager)
    private bool SceneExists(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string scene = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            
            if (scene == sceneName)
                return true;
        }
        return false;
    }

    Vector3 CalcularDireccionCarril()
    {
        if (puntosCarrilActual == null || puntosCarrilActual.Length < 2) 
            return cabezaVR.forward;

        EncontrarPuntoCarrilMasCercano();
        
        for (int i = 0; i < puntosCarrilActual.Length - 1; i++)
        {
            Vector3 inicio = puntosCarrilActual[i].position;
            Vector3 fin = puntosCarrilActual[i + 1].position;
            
            Vector3 puntoMasCercano = PuntoMasCercanoEnSegmento(inicio, fin, transform.position);
            
            if (Vector3.Distance(puntoMasCercano, puntoCarrilMasCercano) < 0.1f)
            {
                return (fin - inicio).normalized;
            }
        }

        return cabezaVR.forward;
    }

    void EncontrarPuntoCarrilMasCercano()
    {
        if (puntosCarrilActual == null || puntosCarrilActual.Length == 0) return;

        float distanciaMasCercana = Mathf.Infinity;
        puntoCarrilMasCercano = puntosCarrilActual[0].position;

        for (int i = 0; i < puntosCarrilActual.Length - 1; i++)
        {
            Vector3 inicio = puntosCarrilActual[i].position;
            Vector3 fin = puntosCarrilActual[i + 1].position;
            
            Vector3 puntoMasCercano = PuntoMasCercanoEnSegmento(inicio, fin, transform.position);
            float distancia = Vector3.Distance(transform.position, puntoMasCercano);
            
            if (distancia < distanciaMasCercana)
            {
                distanciaMasCercana = distancia;
                puntoCarrilMasCercano = puntoMasCercano;
            }
        }
    }

    Vector3 PuntoMasCercanoEnSegmento(Vector3 a, Vector3 b, Vector3 punto)
    {
        Vector3 ab = b - a;
        float t = Vector3.Dot(punto - a, ab) / Vector3.Dot(ab, ab);
        t = Mathf.Clamp01(t);
        return a + t * ab;
    }

    public void ActivarCarril(int index)
    {
        if (index >= 0 && index < carriles.Length)
        {
            carrilActualIndex = index;
            puntosCarrilActual = carriles[index].puntosCarril;
            enPuntoFinal = false;
            Debug.Log($"Carril activado: {carriles[index].nombre}");
        }
    }

    // Método llamado cuando se carga una nueva escena
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Si es una escena de puzzle, activar el flag
        if (scene.name == "Mohan_puzzle" || scene.name == "Bachue_puzzle")
        {
            enEscenaPuzzle = true;
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}