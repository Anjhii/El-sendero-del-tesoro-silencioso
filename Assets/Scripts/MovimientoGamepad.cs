using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Video;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class MovimientoGamepad : MonoBehaviour
{
    public enum EstadoMovimiento { Libre, EnCarril, EnNarrativa, EnPuzzle, Transicionando }
    
    [Header("Estados")]
    public EstadoMovimiento estadoActual = EstadoMovimiento.Libre;
    
    [Header("Movimiento")]
    public float velocidad = 2f;
    public Transform cabeza;
    public InputAction movimiento;

    [Header("Gravedad")]
    public float gravedad = 9.81f;
    private float velocidadVertical = 0f;
    private float velocidadVerticalSuavizada = 0f;
    private float suavizado = 0.1f;

    [Header("Sistema de Carriles")]
    public CarrilData[] carriles;
    private int carrilActualIndex = -1;
    private Transform[] puntosCarrilActual;
    private Vector3 puntoCarrilCercano;
    private float anchoCarrilActual = 2f;
    public float fuerzaAtraccion = 2f;
    public float desviacionMaxima = 3f;

    [Header("Posiciones Específicas")]
    public Transform posicionInicioTemplo;
    public Transform posicionSalidaTemplo;

    [Header("Referencias")]
    public GameManager gameManager;
    public VideoPlayer videoNarrativa;

    private Vector2 input;
    private CharacterController controller;

    [System.Serializable]
    public class CarrilData
    {
        public string nombre;
        public Transform[] puntosCarril;
        public Transform puntoFinal;
        public string nombreTemplo;
    }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Start()
    {
        if (carriles != null && carriles.Length > 0)
        {
            ActivarCarril(0);
        }
        else
        {
            Debug.LogError("No hay carriles asignados al jugador.");
        }
    }

    void OnEnable()
{
    movimiento.Enable();
    movimiento.performed += ctx => input = new Vector2(0, ctx.ReadValue<Vector2>().y);
    movimiento.canceled += ctx => input = Vector2.zero;
}


    void OnDisable()
    {
        movimiento.Disable();
    }

    void Update()
    {
        switch (estadoActual)
        {
            case EstadoMovimiento.Libre:
                MovimientoLibre();
                break;
                
            case EstadoMovimiento.EnCarril:
                MovimientoEnCarril();
                VerificarLlegadaTemplo();
                break;
                
            case EstadoMovimiento.EnNarrativa:
                break;
                
            case EstadoMovimiento.EnPuzzle:
                break;
                
            case EstadoMovimiento.Transicionando:
                break;
        }
    }

    void MovimientoLibre()
    {
        Vector3 direccion = new Vector3(input.x, 0, input.y);
        direccion = cabeza.TransformDirection(direccion);
        direccion.y = 0;

        AplicarGravedad();
        direccion.y = velocidadVerticalSuavizada;

        controller.Move(direccion * velocidad * Time.deltaTime);
    }

    void MovimientoEnCarril()
{
    if (puntosCarrilActual == null || puntosCarrilActual.Length < 2) return;

    // Movimiento solo hacia adelante/atrás del carril según input.y
    Vector3 forward = cabeza.forward;
    forward.y = 0;
    forward.Normalize();

    Vector3 direccion = forward * input.y;

    // Corrección automática al eje del carril
    Vector3 correccion = CalcularCorreccionCarril();
    direccion += correccion;

    AplicarGravedad();
    direccion.y = velocidadVerticalSuavizada;

    controller.Move(direccion * velocidad * Time.deltaTime);
}


Vector3 CalcularCorreccionCarril()
{
    Vector3 posicionJugador = transform.position;
    EncontrarPuntoCarrilCercano(posicionJugador);

    float distanciaAlCarril = Vector3.Distance(posicionJugador, puntoCarrilCercano);

    if (distanciaAlCarril > 0.01f) // siempre se ajusta suavemente
    {
        Vector3 direccionAlCarril = (puntoCarrilCercano - posicionJugador).normalized;
        float fuerza = Mathf.Clamp01(distanciaAlCarril / desviacionMaxima) * fuerzaAtraccion;

        // multiplica para hacerlo fuerte pero suave en VR
        fuerza *= 3f;
        return direccionAlCarril * fuerza;
    }
    return Vector3.zero;
}


    void AplicarGravedad()
    {
        if (controller.isGrounded)
        {
            velocidadVertical = -0.5f;
        }
        else
        {
            velocidadVertical -= gravedad * Time.deltaTime;
        }
        velocidadVerticalSuavizada = Mathf.Lerp(velocidadVerticalSuavizada, velocidadVertical, suavizado);
    }

    public void ActivarCarril(int indexCarril)
    {
        if (indexCarril < 0 || indexCarril >= carriles.Length) 
        {
            Debug.LogError($"Índice de carril inválido: {indexCarril}");
            return;
        }
        
        carrilActualIndex = indexCarril;
        puntosCarrilActual = carriles[indexCarril].puntosCarril;
        estadoActual = EstadoMovimiento.EnCarril;
        
        Debug.Log($"Carril activado: {carriles[indexCarril].nombre}");
        
        // Forzar actualización del punto cercano inmediatamente
        if (puntosCarrilActual != null && puntosCarrilActual.Length > 0)
        {
            EncontrarPuntoCarrilCercano(transform.position);
        }
    }

    void VerificarLlegadaTemplo()
{
    if (estadoActual != EstadoMovimiento.EnCarril || carrilActualIndex == -1) return;

    Transform puntoFinal = carriles[carrilActualIndex].puntoFinal;
    if (puntoFinal == null) return;

    float distancia = Vector3.Distance(transform.position, puntoFinal.position);

    if (distancia < 1.5f) // umbral VR más sensible
    {
        estadoActual = EstadoMovimiento.Transicionando;
        IniciarNarrativaTemplo();
    }
}


    void IniciarNarrativaTemplo()
    {
        estadoActual = EstadoMovimiento.EnNarrativa;
        input = Vector2.zero;
        
        if (videoNarrativa != null)
        {
            videoNarrativa.gameObject.SetActive(true);
            videoNarrativa.Play();
            StartCoroutine(EsperarFinVideo());
        }
        else
        {
            Debug.LogWarning("No hay VideoPlayer asignado. Yendo directamente al templo.");
            EntrarAlTemplo();
        }
    }

    IEnumerator EsperarFinVideo()
    {
        yield return new WaitForSeconds((float)videoNarrativa.length);
        videoNarrativa.gameObject.SetActive(false);
        EntrarAlTemplo();
    }

    void EntrarAlTemplo()
    {
        if (posicionInicioTemplo != null)
        {
            controller.enabled = false;
            transform.position = posicionInicioTemplo.position;
            transform.rotation = posicionInicioTemplo.rotation;
            controller.enabled = true;
        }
        
        estadoActual = EstadoMovimiento.EnPuzzle;
        
        if (gameManager != null)
        {
            string nombreTemplo = carriles[carrilActualIndex].nombreTemplo;
            gameManager.IniciarPuzzle(nombreTemplo);
        }
        else
        {
            Debug.LogError("GameManager no asignado en MovimientoGamepad");
        }
    }

    public void PuzzleTerminado()
    {
        if (posicionSalidaTemplo != null)
        {
            controller.enabled = false;
            transform.position = posicionSalidaTemplo.position;
            transform.rotation = posicionSalidaTemplo.rotation;
            controller.enabled = true;
        }
        
        int siguienteCarril = carrilActualIndex + 1;
        if (siguienteCarril < carriles.Length)
        {
            ActivarCarril(siguienteCarril);
        }
        else
        {
            estadoActual = EstadoMovimiento.Libre;
            Debug.Log("Todos los templos completados!");
        }
    }

    void AplicarCorreccionCarril()
    {
        Vector3 posicionJugador = transform.position;
        EncontrarPuntoCarrilCercano(posicionJugador);

        float distanciaAlCarril = Vector3.Distance(posicionJugador, puntoCarrilCercano);

        if (distanciaAlCarril > anchoCarrilActual * 0.1f)
        {
            Vector3 direccionAlCarril = (puntoCarrilCercano - posicionJugador).normalized;
            float fuerza = Mathf.Clamp01(distanciaAlCarril / desviacionMaxima) * fuerzaAtraccion;
            controller.Move(direccionAlCarril * fuerza * Time.deltaTime);
        }
    }

    void EncontrarPuntoCarrilCercano(Vector3 posicionJugador)
    {
        if (puntosCarrilActual == null || puntosCarrilActual.Length == 0) 
        {
            Debug.LogWarning("No hay puntos de carril asignados");
            return;
        }
        
        float distanciaMasCercana = float.MaxValue;
        puntoCarrilCercano = puntosCarrilActual[0].position;

        for (int i = 0; i < puntosCarrilActual.Length - 1; i++)
        {
            if (puntosCarrilActual[i] == null || puntosCarrilActual[i + 1] == null) 
            {
                Debug.LogWarning($"Puntos de carril nulos en índice {i}");
                continue;
            }
            
            Vector3 inicioSegmento = puntosCarrilActual[i].position;
            Vector3 finSegmento = puntosCarrilActual[i + 1].position;
            
            Vector3 puntoMasCercano = PuntoMasCercanoEnSegmento(inicioSegmento, finSegmento, posicionJugador);
            float distancia = Vector3.Distance(posicionJugador, puntoMasCercano);
            
            if (distancia < distanciaMasCercana)
            {
                distanciaMasCercana = distancia;
                puntoCarrilCercano = puntoMasCercano;
                anchoCarrilActual = 2f; // Puedes hacer esto configurable por carril
            }
        }
        
        // Debug para verificar
        Debug.Log($"Punto carril cercano encontrado: {puntoCarrilCercano}");
    }
    

    Vector3 PuntoMasCercanoEnSegmento(Vector3 a, Vector3 b, Vector3 punto)
    {
        Vector3 ab = b - a;
        float t = Vector3.Dot(punto - a, ab) / Vector3.Dot(ab, ab);
        t = Mathf.Clamp01(t);
        return a + t * ab;
    }

    void OnDrawGizmos()
    {
        // Dibujar todos los carriles en el inspector
        Gizmos.color = Color.blue;
        foreach (var carril in carriles)
        {
            if (carril.puntosCarril != null)
            {
                for (int i = 0; i < carril.puntosCarril.Length - 1; i++)
                {
                    if (carril.puntosCarril[i] != null && carril.puntosCarril[i + 1] != null)
                    {
                        Gizmos.DrawLine(carril.puntosCarril[i].position, carril.puntosCarril[i + 1].position);
                    }
                }
            }
        }
        
        // Dibujar carril activo en verde (si hay uno activo)
        if (puntosCarrilActual != null && puntosCarrilActual.Length >= 2)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < puntosCarrilActual.Length - 1; i++)
            {
                if (puntosCarrilActual[i] != null && puntosCarrilActual[i + 1] != null)
                {
                    Gizmos.DrawLine(puntosCarrilActual[i].position, puntosCarrilActual[i + 1].position);
                    
                    Vector3 direccion = (puntosCarrilActual[i + 1].position - puntosCarrilActual[i].position).normalized;
                    Vector3 perpendicular = Vector3.Cross(direccion, Vector3.up).normalized;
                    
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(puntosCarrilActual[i].position + perpendicular * anchoCarrilActual * 0.5f,
                                puntosCarrilActual[i + 1].position + perpendicular * anchoCarrilActual * 0.5f);
                    Gizmos.DrawLine(puntosCarrilActual[i].position - perpendicular * anchoCarrilActual * 0.5f,
                                puntosCarrilActual[i + 1].position - perpendicular * anchoCarrilActual * 0.5f);
                }
            }
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(puntoCarrilCercano, 0.2f);
        }
    }
    
}