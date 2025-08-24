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
    {  ScreenFader.Instance.FadeToSceneWithTitle(
        sceneName: gameSceneName,
        title: "First Floor",
        outDur: 2f,
        inDur: 1f,
        holdBeforeLoad: 1f,
        holdAfterLoad: 1f,
        titleFadeIn: 1f,   
        titleFadeOut: 1f 
    );
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
