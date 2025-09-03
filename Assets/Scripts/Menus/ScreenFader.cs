using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    // ---------- General ----------
    [Header("Defaults")]
    [Range(0.05f, 6f)] public float defaultFadeDuration = 0.6f;
    public Color fadeColor = Color.black;

    // ---------- Title (used for BETWEEN-SCENES, not boot) ----------
    [Header("Between-Scenes Title")]
    public TMP_FontAsset titleFont;
    public float titleFontSize = 64f;
    public Color titleColor = Color.white;
    public Vector2 titleMargins = new Vector2(64, 64);
    [Range(0.05f, 6f)] public float defaultTitleFadeIn = 0.4f;
    [Range(0.05f, 6f)] public float defaultTitleFadeOut = 0.3f;

    // ---------- Menu Music Out ----------
    [Header("Menu Music Fade-Out")]
    public AudioSource musicToFade;                          // menu AudioSource
    public bool stopAndDestroyMusicOnLoad = true;
    [Range(0.1f, 10f)] public float musicFadeOutDuration = 2f;
    float _musicStartVol = 1f;
    [Range(0f, 3f)] public float bootMusicDelay = 0f; // 0 = immediate, 3 = 3s delay
    public bool playBootMusicOnBoot = true;     // toggle if you ever want it off

    // ---------- Next Scene Music In ----------
    [Header("First Floor Music Fade-In")]
    public AudioClip nextSceneMusic;
    [Range(0f, 1f)] public float nextSceneMusicVolume = 0.6f;
    [Range(0.1f, 10f)] public float nextSceneMusicFadeInDuration = 1.0f;
    AudioSource _sceneMusicSource;

    // ---------- Intertitle SFX (while title is up during transitions) ----------
    [Header("Intertitle SFX")]
    public AudioClip interTitleClip;
    [Range(0f, 1f)] public float interTitleVolume = 0.8f;
    public bool interTitleLoop = true;
    [Range(0.05f, 5f)] public float interTitleFadeIn = 0.2f;
    [Range(0.05f, 5f)] public float interTitleFadeOut = 0.2f;
    AudioSource _interSource;

    // ---------- Boot Splash (PNG instead of text) ----------
    [Header("Boot Splash (PNG)")]
    public bool fadeInOnBoot = true;                         // run boot sequence once
    public Sprite bootImage;
    [Range(0f,   6f)] public float bootHoldBeforeImage = 1f; // black hold before PNG
    [Range(0.05f,6f)] public float bootImageFadeIn  = 0.6f;
    [Range(0f,   6f)] public float bootImageHold    = 0.5f;
    [Range(0.05f,6f)] public float bootImageFadeOut = 0.4f;
    [Range(0.05f,6f)] public float bootScreenFadeIn = 0.6f;  // black -> menu UI

    // ---------- Internals ----------
    CanvasGroup _group;           // screen overlay alpha
    Image _img;                   // full-screen black
    TextMeshProUGUI _titleTMP;    // between-scenes title
    CanvasGroup _titleGroup;      // title alpha
    Image _bootImageUI;           // boot PNG
    CanvasGroup _bootImageGroup;  // boot PNG alpha

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Root canvas
        var canvasGO = new GameObject("FadeCanvas", typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        _group = canvasGO.GetComponent<CanvasGroup>();
        _group.alpha = 1f; // start black for boot
        _img = CreateImage(canvasGO.transform, "FadeImage", fadeColor, fullScreen: true);
        _img.raycastTarget = false;

        // Between-scenes title
        var titleGO = new GameObject("LevelTitle", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(CanvasGroup));
        titleGO.transform.SetParent(canvasGO.transform, false);
        _titleTMP = titleGO.GetComponent<TextMeshProUGUI>();
        _titleGroup = titleGO.GetComponent<CanvasGroup>();
        _titleGroup.alpha = 0f; _titleGroup.interactable = false; _titleGroup.blocksRaycasts = false;
        _titleTMP.alignment = TextAlignmentOptions.Center;
        _titleTMP.enableWordWrapping = false;
        _titleTMP.raycastTarget = false;
        if (titleFont) _titleTMP.font = titleFont;
        _titleTMP.fontSize = titleFontSize;
        _titleTMP.color = titleColor;
        _titleTMP.text = ""; _titleTMP.enabled = false;
        var tr = (RectTransform)titleGO.transform;
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(titleMargins.x, titleMargins.y);
        tr.offsetMax = new Vector2(-titleMargins.x, -titleMargins.y);

        // Boot PNG (fills screen by default; set preserveAspect=true)
        var bootGO = new GameObject("BootImage", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        bootGO.transform.SetParent(canvasGO.transform, false);
        _bootImageUI = bootGO.GetComponent<Image>();
        _bootImageGroup = bootGO.GetComponent<CanvasGroup>();
        _bootImageUI.sprite = bootImage;
        _bootImageUI.preserveAspect = true;
        _bootImageUI.raycastTarget = false;
        _bootImageUI.enabled = false;
        _bootImageGroup.alpha = 0f;
        var br = (RectTransform)bootGO.transform;
        br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one;
        br.offsetMin = Vector2.zero; br.offsetMax = Vector2.zero;
    }

    void Start()
    {
        if (playBootMusicOnBoot) StartCoroutine(StartBootMusicAfterDelay());
        if (fadeInOnBoot) StartCoroutine(BootSequence());
        else StartCoroutine(FadeRoutine(0f, defaultFadeDuration));
    }

    // ---------- Public API ----------
    public void FadeToScene(string sceneName, float outDur = -1f, float inDur = -1f)
        => StartCoroutine(LoadSequence(sceneName,
            outDur < 0 ? defaultFadeDuration : outDur,
            inDur  < 0 ? defaultFadeDuration : inDur));

    public void FadeToSceneWithTitle(
        string sceneName, string title,
        float outDur = -1f, float holdBeforeLoad = 0.75f, float holdAfterLoad = 0.25f,
        float inDur = -1f, float titleFadeIn = -1f, float titleFadeOut = -1f)
        => StartCoroutine(LoadSequenceWithTitle(
            sceneName,
            string.IsNullOrEmpty(title) ? sceneName : title,
            outDur      < 0 ? defaultFadeDuration   : outDur,
            Mathf.Max(0f, holdBeforeLoad),
            Mathf.Max(0f, holdAfterLoad),
            inDur       < 0 ? defaultFadeDuration   : inDur,
            titleFadeIn < 0 ? defaultTitleFadeIn    : titleFadeIn,
            titleFadeOut< 0 ? defaultTitleFadeOut   : titleFadeOut));

    public Coroutine FadeOut(float duration) => StartCoroutine(FadeRoutine(1f, duration));
    public Coroutine FadeIn (float duration) => StartCoroutine(FadeRoutine(0f, duration));

    // ---------- Scene flows ----------
    IEnumerator LoadSequence(string sceneName, float outDur, float inDur)
    {
        yield return FadeOut(outDur);

        var op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone) yield return null;

        if (musicToFade && stopAndDestroyMusicOnLoad)
        {
            musicToFade.Stop();
            Destroy(musicToFade.gameObject);
            musicToFade = null;
        }

        StartCoroutine(FadeInNextSceneMusic());
        yield return null;
        yield return FadeIn(inDur);
    }

    IEnumerator LoadSequenceWithTitle(string sceneName, string title,
        float outDur, float holdBefore, float holdAfter, float inDur, float titleIn, float titleOut)
    {
        yield return FadeOut(outDur);

        // title in + intertitle SFX
        _titleTMP.text = title;
        _titleTMP.enabled = true;
        _titleGroup.alpha = 0f;
        StartCoroutine(InterFadeIn());
        yield return FadeCanvasGroup(_titleGroup, 1f, titleIn);

        if (holdBefore > 0f) yield return new WaitForSecondsRealtime(holdBefore);

        var op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone) yield return null;

        if (musicToFade && stopAndDestroyMusicOnLoad)
        {
            musicToFade.Stop();
            Destroy(musicToFade.gameObject);
            musicToFade = null;
        }

        if (holdAfter > 0f) yield return new WaitForSecondsRealtime(holdAfter);

        // title out + intertitle SFX out
        yield return FadeCanvasGroup(_titleGroup, 0f, titleOut);
        yield return InterFadeOutAndStop();
        _titleTMP.enabled = false;

        StartCoroutine(FadeInNextSceneMusic());
        yield return FadeIn(inDur);
    }

    // ---------- Core fades ----------
    IEnumerator FadeRoutine(float target, float duration)
    {
        if (duration <= 0f)
        {
            _group.alpha = target;
            _img.raycastTarget = _group.alpha > 0.001f;
            if (musicToFade && target >= 1f) musicToFade.volume = 0f;
            yield break;
        }

        float start = _group.alpha, t = 0f;
        if (musicToFade) _musicStartVol = musicToFade.volume;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            float a = Mathf.Lerp(start, target, t);
            _group.alpha = a;
            _img.raycastTarget = a > 0.001f;

            if (musicToFade && target >= 1f)
            {
                float musicT = Mathf.Clamp01(t * (duration / Mathf.Max(0.0001f, musicFadeOutDuration)));
                musicToFade.volume = Mathf.Lerp(_musicStartVol, 0f, musicT);
            }
            yield return null;
        }

        _group.alpha = target;
        _img.raycastTarget = _group.alpha > 0.001f;
        if (musicToFade && target >= 1f) musicToFade.volume = 0f;
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float target, float duration)
    {
        if (!cg) yield break;
        if (duration <= 0f) { cg.alpha = target; yield break; }
        float start = cg.alpha, t = 0f, dur = Mathf.Max(0.0001f, duration);
        while (t < 1f) { t += Time.unscaledDeltaTime / dur; cg.alpha = Mathf.Lerp(start, target, t); yield return null; }
        cg.alpha = target;
    }

    // ---------- Boot splash (PNG) ----------
    IEnumerator BootSequence()
    {
        _group.alpha = 1f; _img.raycastTarget = true;

        if (bootHoldBeforeImage > 0f)
            yield return new WaitForSecondsRealtime(bootHoldBeforeImage);

        if (bootImage && _bootImageUI && _bootImageGroup)
        {
            _bootImageUI.enabled = true;
            yield return FadeCanvasGroup(_bootImageGroup, 1f, bootImageFadeIn);
            if (bootImageHold > 0f) yield return new WaitForSecondsRealtime(bootImageHold);
            yield return FadeCanvasGroup(_bootImageGroup, 0f, bootImageFadeOut);
            _bootImageUI.enabled = false;
        }

        yield return FadeIn(bootScreenFadeIn);
    }

    // ---------- Music helpers ----------
    void EnsureSceneMusicSource()
    {
        if (_sceneMusicSource) return;
        _sceneMusicSource = gameObject.AddComponent<AudioSource>();
        _sceneMusicSource.playOnAwake = false;
        _sceneMusicSource.loop = true;
        _sceneMusicSource.spatialBlend = 0f;
        _sceneMusicSource.volume = 0f;
    }

    IEnumerator FadeInNextSceneMusic()
    {
        if (!nextSceneMusic) yield break;
        EnsureSceneMusicSource();
        _sceneMusicSource.Stop();
        _sceneMusicSource.clip = nextSceneMusic;
        _sceneMusicSource.volume = 0f;
        _sceneMusicSource.Play();

        float t = 0f, dur = Mathf.Max(0.0001f, nextSceneMusicFadeInDuration);
        while (t < 1f) { t += Time.unscaledDeltaTime / dur; _sceneMusicSource.volume = Mathf.Lerp(0f, nextSceneMusicVolume, t); yield return null; }
        _sceneMusicSource.volume = nextSceneMusicVolume;
    }

    // ---------- Intertitle SFX helpers ----------
    void EnsureInterSource()
    {
        if (_interSource) return;
        _interSource = gameObject.AddComponent<AudioSource>();
        _interSource.playOnAwake = false;
        _interSource.loop = interTitleLoop;
        _interSource.spatialBlend = 0f;
        _interSource.volume = 0f;
    }

    IEnumerator InterFadeIn()
    {
        if (!interTitleClip) yield break;
        EnsureInterSource();
        _interSource.Stop();
        _interSource.clip = interTitleClip;
        _interSource.loop = interTitleLoop;
        _interSource.volume = 0f;
        _interSource.Play();

        float t = 0f, dur = Mathf.Max(0.0001f, interTitleFadeIn);
        while (t < 1f) { t += Time.unscaledDeltaTime / dur; _interSource.volume = Mathf.Lerp(0f, interTitleVolume, t); yield return null; }
        _interSource.volume = interTitleVolume;
    }

    IEnumerator InterFadeOutAndStop()
    {
        if (!_interSource || !_interSource.isPlaying) yield break;
        float start = _interSource.volume, t = 0f, dur = Mathf.Max(0.0001f, interTitleFadeOut);
        while (t < 1f) { t += Time.unscaledDeltaTime / dur; _interSource.volume = Mathf.Lerp(start, 0f, t); yield return null; }
        _interSource.volume = 0f; _interSource.Stop();
    }

    IEnumerator StartBootMusicAfterDelay()
    {
        if (!musicToFade) yield break;

        // If the source was set to Play On Awake, prevent instant start when delaying
        if (bootMusicDelay > 0f && musicToFade.isPlaying) musicToFade.Stop();

        if (bootMusicDelay > 0f)
            yield return new WaitForSecondsRealtime(bootMusicDelay);

        if (!musicToFade.isPlaying) musicToFade.Play();
    }

    

    // ---------- Small util ----------
    Image CreateImage(Transform parent, string name, Color color, bool fullScreen = true)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        if (fullScreen)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
        return img;
    }
}
