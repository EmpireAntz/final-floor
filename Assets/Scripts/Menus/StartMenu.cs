using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class StartMenu : MonoBehaviour
{
    [Header("Scene")]
    public string gameSceneName = "LevelOne";  // 👈 now loads LevelOne

    [Header("UI")]
    public GameObject firstSelected;

    void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (firstSelected) EventSystem.current.SetSelectedGameObject(firstSelected);
    }

    public void OnPlay()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
