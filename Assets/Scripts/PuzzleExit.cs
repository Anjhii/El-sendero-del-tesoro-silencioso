using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PuzzleExit : MonoBehaviour
{
    [Header("Configuración")]
    public InputActionReference teclaSalir; // Asigna la tecla Y de tu Input Asset
    public string escenaDestino = "TemplosScene";
    
    void Start()
    {
        // Habilitar la tecla de salida
        teclaSalir.action.Enable();
        Debug.Log("Puzzle cargado. Presiona Y para regresar.");
    }
    
    void Update()
    {
        // Verificar si se presiona la tecla Y
        if (teclaSalir.action.triggered)
        {
            RegresarATemplos();
        }
    }
    
    void RegresarATemplos()
    {
        Debug.Log("Regresando a TemplosScene...");
        SceneManager.LoadScene(escenaDestino);
    }
    
    void OnDestroy()
    {
        // Limpiar al destruir el objeto
        if (teclaSalir != null)
        {
            teclaSalir.action.Disable();
        }
    }
}