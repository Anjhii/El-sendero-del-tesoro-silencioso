using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Carga y descarga escenas del juego
/// </summary>
public class SceneLoader : MonoBehaviour
{
    [Header("Scene Names")]
    [Tooltip("Nombre de la escena del Bubble Shooter")]
    [SerializeField] private string bubbleShooterSceneName = "PuzzleBachue";
    
    [Tooltip("Nombre de la escena principal")]
    [SerializeField] private string mainSceneName = "MainScene";

    [Header("Debug")]
    [SerializeField] private bool enableDebugKeys = true;

    private void Update()
    {
        if (!enableDebugKeys) return;

        // Teclas de debug para testing en el editor
        if (Input.GetKeyDown(KeyCode.B))
        {
            LoadBubbleShooter();
        }
        
        if (Input.GetKeyDown(KeyCode.M))
        {
            ReturnToMainScene();
        }
    }

    /// <summary>
    /// Carga la escena del Bubble Shooter (modo simple)
    /// </summary>
    public void LoadBubbleShooter()
    {
        Debug.Log($"Cargando escena: {bubbleShooterSceneName}");
        SceneManager.LoadScene(bubbleShooterSceneName);
    }

    /// <summary>
    /// Carga la escena del Bubble Shooter (modo asíncrono con progreso)
    /// </summary>
    public void LoadBubbleShooterAsync()
    {
        StartCoroutine(LoadSceneAsyncCoroutine(bubbleShooterSceneName));
    }

    /// <summary>
    /// Descarga la escena del Bubble Shooter (útil para escenas aditivas)
    /// </summary>
    public void UnloadBubbleShooter()
    {
        Debug.Log($"Descargando escena: {bubbleShooterSceneName}");
        SceneManager.UnloadSceneAsync(bubbleShooterSceneName);
    }

    /// <summary>
    /// Regresa a la escena principal
    /// </summary>
    public void ReturnToMainScene()
    {
        Debug.Log($"Regresando a escena: {mainSceneName}");
        SceneManager.LoadScene(mainSceneName);
    }

    /// <summary>
    /// Carga cualquier escena por nombre
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("SceneLoader: Nombre de escena vacío!");
            return;
        }

        Debug.Log($"Cargando escena: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Carga una escena aditivamente (sin descargar la actual)
    /// </summary>
    public void LoadSceneAdditive(string sceneName)
    {
        Debug.Log($"Cargando escena aditiva: {sceneName}");
        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
    }

    /// <summary>
    /// Corrutina para carga asíncrona con barra de progreso
    /// </summary>
    private IEnumerator LoadSceneAsyncCoroutine(string sceneName)
    {
        Debug.Log($"Iniciando carga asíncrona de: {sceneName}");
        
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        
        // Opcional: prevenir activación automática
        asyncLoad.allowSceneActivation = false;
        
        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            Debug.Log($"Progreso de carga: {progress * 100}%");
            
            // Cuando llegue al 90%, activar la escena
            if (asyncLoad.progress >= 0.9f)
            {
                Debug.Log("Carga completa. Activando escena...");
                asyncLoad.allowSceneActivation = true;
            }
            
            yield return null;
        }
        
        Debug.Log($"Escena {sceneName} cargada exitosamente!");
    }

    /// <summary>
    /// Reinicia la escena actual
    /// </summary>
    public void RestartCurrentScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        Debug.Log($"Reiniciando escena: {currentScene}");
        SceneManager.LoadScene(currentScene);
    }

    /// <summary>
    /// Sale del juego (funciona en build, no en editor)
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Saliendo del juego...");
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}