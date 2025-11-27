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
    public InputActionReference accionInteractuar;

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
    private bool inputHabilitado = true;

    [System.Serializable]
    public class CarrilData
    {
        public string nombre;
        public Transform[] puntosCarril;
        public float anchoCarril = 2f;
        public Transform puntoFinal;
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        velocidadActual = velocidadMovimiento;
        
        if (GameManager.Instance != null)
        {
            int carrilAIniciar = GameManager.Instance.siguienteIndiceCarril;
            
            if (carrilAIniciar > 0)
            {
                ConfigurarPosicionInicial(carrilAIniciar);
            }
            else
            {
                ActivarCarril(0);
            }
        }
        else
        {
            ActivarCarril(0);
        }
    }

    void OnEnable()
    {
        if (accionMovimiento != null) accionMovimiento.action.Enable();
        if (accionInteractuar != null) accionInteractuar.action.Enable();
    }

    void OnDisable()
    {
        if (accionMovimiento != null) accionMovimiento.action.Disable();
        if (accionInteractuar != null) accionInteractuar.action.Disable();
    }

    void Update()
    {
        if (!inputHabilitado) return;
        
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

    void ConfigurarPosicionInicial(int indiceCarril)
    {
        Transform destino = null;

        switch (indiceCarril)
        {
            case 1:
                destino = posicionInicioCarril2;
                break;
            case 2:
                destino = posicionInicioCarril3;
                break;
            case 3:
                destino = posicionFinalCarril3;
                break;
        }

        if (destino != null)
        {
            controller.enabled = false;
            transform.position = destino.position;
            transform.rotation = destino.rotation;
            controller.enabled = true;
            ActivarCarril(indiceCarril);
        }
        else
        {
            ActivarCarril(0);
        }
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
            Debug.Log($"¡Llegaste al final! Presiona A para interactuar.");
        }
        else if (distanciaAlFinal >= 1.5f && enPuntoFinal)
        {
            enPuntoFinal = false;
        }
    }

    void ProcesarInputInteraccion()
    {
        if (accionInteractuar.action.triggered && enPuntoFinal && inputHabilitado)
        {
            ManejarInteraccionPuntoFinal();
        }
    }

    void ManejarInteraccionPuntoFinal()
        {
            inputHabilitado = false; // Deshabilitar input durante la transición
            
            switch (carrilActualIndex)
            {
                case 0: // Final del Carril 1 -> Va a Mohan
                    if (GameManager.Instance != null)
                        GameManager.Instance.GuardarProgresoCarril(1);
                    CargarEscenaPuzzle("Mohan_Puzzle");
                    break;
                    
                case 1: // Final del Carril 2 -> Va a Bachue
                    if (GameManager.Instance != null)
                        GameManager.Instance.GuardarProgresoCarril(2);
                    CargarEscenaPuzzle("Bachue_Puzzle");
                    break;
                    
                case 2: // Final del Carril 3 -> Va a BarcoScene
                    Debug.Log("Final del Carril 3 alcanzado. Cargando barco...");
                    // Usamos el mismo método de carga para ir a la escena del barco
                    CargarEscenaPuzzle("BarcoScene");
                    break;
            }
            
            enPuntoFinal = false;
        }

    void CargarEscenaPuzzle(string nombreEscena)
    {
        if (SceneExists(nombreEscena))
        {
            Debug.Log($"Cargando puzzle: {nombreEscena}");
            
            // Limpiar inputs antes de cambiar escena
            InputSystem.DisableAllEnabledActions();
            
            SceneManager.LoadScene(nombreEscena);
        }
        else
        {
            Debug.LogError($"Escena '{nombreEscena}' no encontrada!");
            inputHabilitado = true; // Rehabilitar input si falla
        }
    }

    private bool SceneExists(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;
        
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string scene = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            
            if (scene == sceneName)
                return true;
        }
        
        // Verificar también si la escena está cargada actualmente
        Scene sceneActual = SceneManager.GetActiveScene();
        if (sceneActual.name == sceneName)
            return true;
            
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
        }
    }
}