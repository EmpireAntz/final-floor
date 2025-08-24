
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [Header("Defaults")]
    [Range(0.05f, 3f)] public float defaultFadeDuration = 0.6f;
    public Color fadeColor = Color.black;
    public TMP_FontAsset titleFont;               
    public float titleFontSize = 64f;            
    public Color titleColor = Color.white;         
    public Vector2 titleMargins = new Vector2(64, 64); 


    CanvasGroup _group;
    Image _img;
    TextMeshProUGUI _titleTMP;

    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Overlay canvas
        var canvasGO = new GameObject("FadeCanvas", typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        _group = canvasGO.GetComponent<CanvasGroup>();
        _group.alpha = 0f;

        // Fullscreen black image
        var imgGO = new GameObject("FadeImage", typeof(Image));
        imgGO.transform.SetParent(canvasGO.transform, false);
        _img = imgGO.GetComponent<Image>();
        _img.color = fadeColor;
        _img.raycastTarget = false;

        var rt = (RectTransform)imgGO.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        // Title TMP (centered)
        var titleGO = new GameObject("LevelTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(canvasGO.transform, false);
        _titleTMP = titleGO.GetComponent<TextMeshProUGUI>();
        _titleTMP.alignment = TextAlignmentOptions.Center;
        _titleTMP.fontSize = 64f;
        _titleTMP.color = Color.white;
        _titleTMP.text = "";
        _titleTMP.enableWordWrapping = false;
        _titleTMP.raycastTarget = false;
        _titleTMP.enabled = false;

         if (titleFont) _titleTMP.font = titleFont;
        _titleTMP.fontSize = titleFontSize;
        _titleTMP.color    = titleColor;

        var trt = (RectTransform)titleGO.transform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; // fill screen
        trt.offsetMin = new Vector2(64, 64);
        trt.offsetMax = new Vector2(-64, -64);
    }

    // Simple fade → load → fade in
    public void FadeToScene(string sceneName, float outDur = -1f, float inDur = -1f)
        => StartCoroutine(LoadSequence(sceneName,
            outDur < 0 ? defaultFadeDuration : outDur,
            inDur  < 0 ? defaultFadeDuration : inDur));

    // Title card version (fade to black, show title, load, fade in)
    public void FadeToSceneWithTitle(
        string sceneName,
        string title,
        float outDur = -1f,
        float holdBeforeLoad = 0.75f,
        float holdAfterLoad  = 0.25f,
        float inDur = -1f)
    {
        StartCoroutine(LoadSequenceWithTitle(
            sceneName,
            string.IsNullOrEmpty(title) ? sceneName : title,
            outDur < 0 ? defaultFadeDuration : outDur,
            Mathf.Max(0f, holdBeforeLoad),
            Mathf.Max(0f, holdAfterLoad),
            inDur < 0 ? defaultFadeDuration : inDur));
    }

    public Coroutine FadeOut(float duration) => StartCoroutine(FadeRoutine(1f, duration));
    public Coroutine FadeIn (float duration) => StartCoroutine(FadeRoutine(0f, duration));

    IEnumerator LoadSequence(string sceneName, float outDur, float inDur)
    {
        yield return FadeOut(outDur);
        var op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone) yield return null;
        yield return null; // settle one frame
        yield return FadeIn(inDur);
    }

    IEnumerator LoadSequenceWithTitle(string sceneName, string title, float outDur, float holdBefore, float holdAfter, float inDur)
    {
        // Fade to black
        yield return FadeOut(outDur);

        // Show title on black
        if (_titleTMP)
        {
            _titleTMP.text = title;
            _titleTMP.enabled = true;
        }

        // Hold before loading (still black)
        if (holdBefore > 0f) yield return new WaitForSecondsRealtime(holdBefore);

        // Load scene while staying black with title visible
        var op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone) yield return null;

        // Optional short hold after load (still black)
        if (holdAfter > 0f) yield return new WaitForSecondsRealtime(holdAfter);

        // Hide title and fade back to gameplay
        if (_titleTMP) _titleTMP.enabled = false;
        yield return FadeIn(inDur);
    }

    IEnumerator FadeRoutine(float target, float duration)
    {
        if (duration <= 0f) { _group.alpha = target; _img.raycastTarget = target > 0.001f; yield break; }

        float start = _group.alpha;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration; // unaffected by timeScale
            _group.alpha = Mathf.Lerp(start, target, t);
            _img.raycastTarget = _group.alpha > 0.001f; // block clicks while visible
            yield return null;
        }
        _group.alpha = target;
        _img.raycastTarget = _group.alpha > 0.001f;
    }
}
