using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoginManager : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private string templesSceneName = "TemplosScene";
    
    void Start()
    {
        playButton.onClick.AddListener(OnPlayButtonClicked);
    }
    
    public void OnPlayButtonClicked()
    {
        // Verificar si la escena existe
        if (SceneExists(templesSceneName))
        {
            SceneManager.LoadScene(templesSceneName);
        }
        else
        {
            Debug.LogError($"Escena '{templesSceneName}' no encontrada en Build Settings!");
        }
    }
    
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
}