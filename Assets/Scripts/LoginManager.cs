using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // 👈 Importante

public class LoginManager : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private string templesSceneName = "TemplosScene";
    public InputActionReference playAction;

    void OnEnable()
    {
        playAction.action.Enable();
        playAction.action.performed += OnPlayPerformed;
    }

    void OnDisable()
    {
        playAction.action.performed -= OnPlayPerformed;
        playAction.action.Disable();
    }


    void Start()
    {
        // Mantienes el botón UI también
        playButton.onClick.AddListener(OnPlayButtonClicked);
    }

    private void OnPlayPerformed(InputAction.CallbackContext ctx)
    {
        OnPlayButtonClicked();
    }

    public void OnPlayButtonClicked()
    {
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
