using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    // ===================== Defaults =====================
    [Header("Defaults")]
    [Range(0.05f, 3f)] public float defaultFadeDuration = 0.6f;
    public Color fadeColor = Color.black;

    // ===================== Title Defaults =====================
    [Header("Title Defaults")]
    public TMP_FontAsset titleFont;
    public float titleFontSize = 64f;
    public Color titleColor = Color.white;
    public Vector2 titleMargins = new Vector2(64, 64);
    [Range(0.05f, 3f)] public float defaultTitleFadeIn = 0.4f;
    [Range(0.05f, 3f)] public float defaultTitleFadeOut = 0.3f;

    // ===================== Audio (fade out current) =====================
    [Header("Audio Fade (Menu -> Next Scene)")]
    public AudioSource musicToFade;                                  // assign your Menu music AudioSource
    public bool stopAndDestroyMusicOnLoad = true;                    // destroy menu music object after load
    [Range(0.1f, 10f)] public float musicFadeOutDuration = 2f;       // independent from screen fade
    float _musicStartVol = 1f;

    // ===================== Next Scene Music (fade in) =====================
    [Header("Next Scene Music")]
    public AudioClip nextSceneMusic;                                 // set before calling FadeToScene...
    [Range(0f, 1f)] public float nextSceneMusicVolume = 0.6f;
    [Range(0.1f, 10f)] public float nextSceneMusicFadeInDuration = 1.0f;
    AudioSource _sceneMusicSource;                                   // internal 2D music source

    // ===================== Intertitle SFX =====================
    [Header("Intertitle SFX (plays while title is up)")]
    public AudioClip interTitleClip;                 // ambience/stinger to play under title
    [Range(0f,1f)] public float interTitleVolume = 0.8f;
    public bool interTitleLoop = true;
    [Range(0.05f,5f)] public float interTitleFadeIn = 0.2f;
    [Range(0.05f,5f)] public float interTitleFadeOut = 0.2f;
    AudioSource _interSource;                        // internal 2D source for intertitle only

    // ===================== Boot Splash =====================
    [Header("Boot Splash (optional)")]
    public bool fadeInOnBoot = true;                                 // start black, then reveal menu
    [TextArea] public string bootTitle = "";                         // shown over black
    [Range(0f, 6f)] public float bootScreenHoldBeforeTitle = 1f;     // stay black before showing title
    [Range(0.05f, 6f)] public float bootTitleFadeIn  = 0.6f;
    [Range(0f,   6f)] public float bootTitleHold    = 0.5f;
    [Range(0.05f, 6f)] public float bootTitleFadeOut = 0.4f;
    [Range(0.05f, 6f)] public float bootScreenFadeIn = 0.6f;         // black -> menu UI

    // ===================== Internals =====================
    CanvasGroup _group;
    Image _img;
    TextMeshProUGUI _titleTMP;
    CanvasGroup _titleGroup;

    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Overlay canvas (on this persistent GO)
        var canvasGO = new GameObject("FadeCanvas", typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        _group = canvasGO.GetComponent<CanvasGroup>();
        _group.alpha = 1f; // start fully black for boot splash

        // Fullscreen black image
        var imgGO = new GameObject("FadeImage", typeof(Image));
        imgGO.transform.SetParent(canvasGO.transform, false);
        _img = imgGO.GetComponent<Image>();
        _img.color = fadeColor;
        _img.raycastTarget = false;

        var rt = (RectTransform)imgGO.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        // Title (TMP + CanvasGroup)
        var titleGO = new GameObject("LevelTitle", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(CanvasGroup));
        titleGO.transform.SetParent(canvasGO.transform, false);
        _titleTMP = titleGO.GetComponent<TextMeshProUGUI>();
        _titleGroup = titleGO.GetComponent<CanvasGroup>();
        _titleGroup.alpha = 0f;
        _titleGroup.interactable = false;
        _titleGroup.blocksRaycasts = false;

        _titleTMP.alignment = TextAlignmentOptions.Center;
        _titleTMP.enableWordWrapping = false;
        _titleTMP.raycastTarget = false;
        if (titleFont) _titleTMP.font = titleFont;
        _titleTMP.fontSize = titleFontSize;
        _titleTMP.color = titleColor;
        _titleTMP.text = "";
        _titleTMP.enabled = false;

        var trt = (RectTransform)titleGO.transform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(titleMargins.x, titleMargins.y);
        trt.offsetMax = new Vector2(-titleMargins.x, -titleMargins.y);
    }

    void Start()
    {
        if (fadeInOnBoot) StartCoroutine(BootSequence());
        else StartCoroutine(FadeRoutine(0f, defaultFadeDuration));
    }

    // ===================== Public API =====================
    public void FadeToScene(string sceneName, float outDur = -1f, float inDur = -1f)
        => StartCoroutine(LoadSequence(
            sceneName,
            outDur < 0 ? defaultFadeDuration : outDur,
            inDur  < 0 ? defaultFadeDuration : inDur));

    public void FadeToSceneWithTitle(
        string sceneName,
        string title,
        float outDur = -1f,
        float holdBeforeLoad = 0.75f,
        float holdAfterLoad  = 0.25f,
        float inDur = -1f,
        float titleFadeIn = -1f,
        float titleFadeOut = -1f)
    {
        StartCoroutine(LoadSequenceWithTitle(
            sceneName,
            string.IsNullOrEmpty(title) ? sceneName : title,
            outDur < 0 ? defaultFadeDuration : outDur,
            Mathf.Max(0f, holdBeforeLoad),
            Mathf.Max(0f, holdAfterLoad),
            inDur < 0 ? defaultFadeDuration : inDur,
            titleFadeIn  < 0 ? defaultTitleFadeIn  : titleFadeIn,
            titleFadeOut < 0 ? defaultTitleFadeOut : titleFadeOut));
    }

    public Coroutine FadeOut(float duration) => StartCoroutine(FadeRoutine(1f, duration));
    public Coroutine FadeIn (float duration) => StartCoroutine(FadeRoutine(0f, duration));

    // ===================== Scene Load Routines =====================
    IEnumerator LoadSequence(string sceneName, float outDur, float inDur)
    {
        // fade screen to black (music fades using musicFadeOutDuration)
        yield return FadeOut(outDur);

        // load
        var op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone) yield return null;

        // stop/destroy menu music (if provided)
        if (musicToFade && stopAndDestroyMusicOnLoad)
        {
            musicToFade.Stop();
            Destroy(musicToFade.gameObject);
            musicToFade = null;
        }

        // start next scene music fade-in (runs alongside FadeIn)
        StartCoroutine(FadeInNextSceneMusic());

        yield return null; // settle one frame
        yield return FadeIn(inDur);
    }

    IEnumerator LoadSequenceWithTitle(
        string sceneName,
        string title,
        float outDur,
        float holdBefore,
        float holdAfter,
        float inDur,
        float titleIn,
        float titleOut)
    {
        // fade to black (music fades using musicFadeOutDuration)
        yield return FadeOut(outDur);

        // show & fade in the title over black
        if (_titleTMP)
        {
            _titleTMP.text = title;
            _titleTMP.enabled = true;
            _titleGroup.alpha = 0f;

            // start intertitle SFX while title fades in
            StartCoroutine(InterFadeIn());
            yield return FadeTitle(1f, titleIn);
        }

        // hold before loading
        if (holdBefore > 0f) yield return new WaitForSecondsRealtime(holdBefore);

        // load
        var op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone) yield return null;

        // stop/destroy menu music
        if (musicToFade && stopAndDestroyMusicOnLoad)
        {
            musicToFade.Stop();
            Destroy(musicToFade.gameObject);
            musicToFade = null;
        }

        // optional hold after load (still black / title visible)
        if (holdAfter > 0f) yield return new WaitForSecondsRealtime(holdAfter);

        // fade out title (and intertitle SFX), then reveal gameplay
        if (_titleTMP)
        {
            yield return FadeTitle(0f, titleOut);
            yield return StartCoroutine(InterFadeOutAndStop());
            _titleTMP.enabled = false;
        }

        // begin next scene music fade-in, then reveal gameplay
        StartCoroutine(FadeInNextSceneMusic());
        yield return FadeIn(inDur);
    }

    // ===================== Core Fades =====================
    IEnumerator FadeRoutine(float target, float duration)
    {
        if (duration <= 0f)
        {
            _group.alpha = target;
            _img.raycastTarget = _group.alpha > 0.001f;
            if (musicToFade && target >= 1f) musicToFade.volume = 0f;
            yield break;
        }

        float start = _group.alpha;
        float t = 0f;

        if (musicToFade) _musicStartVol = musicToFade.volume;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;

            // screen
            float a = Mathf.Lerp(start, target, t);
            _group.alpha = a;
            _img.raycastTarget = a > 0.001f;

            // independent music fade (only when fading to black)
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

    IEnumerator FadeTitle(float target, float duration)
    {
        if (!_titleGroup) yield break;
        if (duration <= 0f) { _titleGroup.alpha = target; yield break; }

        float start = _titleGroup.alpha;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            _titleGroup.alpha = Mathf.Lerp(start, target, t);
            yield return null;
        }
        _titleGroup.alpha = target;
    }

    // ===================== Boot Splash =====================
    IEnumerator BootSequence()
    {
        // start fully black and block clicks
        _group.alpha = 1f;
        _img.raycastTarget = true;

        // hold on black
        if (bootScreenHoldBeforeTitle > 0f)
            yield return new WaitForSecondsRealtime(bootScreenHoldBeforeTitle);

        // title: fade in, hold, fade out (still on black)
        if (!string.IsNullOrEmpty(bootTitle) && _titleTMP && _titleGroup)
        {
            _titleTMP.text = bootTitle;
            _titleTMP.enabled = true;
            _titleGroup.alpha = 0f;

            yield return FadeTitle(1f, bootTitleFadeIn);

            if (bootTitleHold > 0f)
                yield return new WaitForSecondsRealtime(bootTitleHold);

            yield return FadeTitle(0f, bootTitleFadeOut);
            _titleTMP.enabled = false;
        }

        // finally fade the black overlay away to reveal the menu UI
        yield return FadeIn(bootScreenFadeIn);
    }

    // ===================== Next Scene Music Helpers =====================
    void EnsureSceneMusicSource()
    {
        if (_sceneMusicSource) return;
        _sceneMusicSource = gameObject.AddComponent<AudioSource>();
        _sceneMusicSource.playOnAwake = false;
        _sceneMusicSource.loop = true;
        _sceneMusicSource.spatialBlend = 0f; // 2D
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

        float dur = Mathf.Max(0.0001f, nextSceneMusicFadeInDuration);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            _sceneMusicSource.volume = Mathf.Lerp(0f, nextSceneMusicVolume, t);
            yield return null;
        }
        _sceneMusicSource.volume = nextSceneMusicVolume;
    }

    // ===================== Intertitle SFX Helpers =====================
    void EnsureInterSource()
    {
        if (_interSource) return;
        _interSource = gameObject.AddComponent<AudioSource>();
        _interSource.playOnAwake = false;
        _interSource.loop = interTitleLoop;
        _interSource.spatialBlend = 0f; // 2D
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

        float dur = Mathf.Max(0.0001f, interTitleFadeIn);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            _interSource.volume = Mathf.Lerp(0f, interTitleVolume, t);
            yield return null;
        }
        _interSource.volume = interTitleVolume;
    }

    IEnumerator InterFadeOutAndStop()
    {
        if (_interSource == null || !_interSource.isPlaying) yield break;

        float start = _interSource.volume;
        float dur = Mathf.Max(0.0001f, interTitleFadeOut);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            _interSource.volume = Mathf.Lerp(start, 0f, t);
            yield return null;
        }
        _interSource.volume = 0f;
        _interSource.Stop();
    }
}
