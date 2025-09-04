using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] TMP_Text text;   // drag the child "Text" TMP here

    [Header("Look & Feel")]
    public Color normalColor = Color.white;
    public Color critColor   = new Color(1f, 0.35f, 0.35f);
    public float lifetime    = 0.8f;          // seconds
    public float riseSpeed   = 1.0f;          // world units / sec
    public Vector3 jitter    = new(0.2f, 0.2f, 0.2f);
    public float popScale    = 1.2f;
    public bool billboardToCamera = true;

    // internal
    Vector3 _anchor;
    float   _age;
    Color   _startColor;
    Vector3 _baseScale;
    bool    _valid;

    /// <summary>Initialize popup at a world position.</summary>
    public void Init(int amount, bool isCrit, Vector3 spawnWorldPos)
    {
        // 1) Ensure no parent + sane transform on ROOT (world)
        if (transform.parent != null) transform.SetParent(null, true);
        transform.position   = spawnWorldPos;
        transform.rotation   = Quaternion.identity;
        transform.localScale = Vector3.one;

        // 2) Ensure we have TMP text; also zero the CHILD local transform
        if (!text) text = GetComponentInChildren<TMP_Text>(true);
        if (!text) { Debug.LogError("DamagePopup: Missing TMP_Text child.", this); Destroy(gameObject); return; }

        var t = text.transform;                  // the child
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
        t.localScale    = Vector3.one;

        // 3) Fixed anchor + tiny random jitter (world space)
        _anchor = spawnWorldPos + new Vector3(
            Random.Range(-jitter.x, jitter.x),
            Random.Range(0f,        jitter.y),
            Random.Range(-jitter.z, jitter.z)
        );

        // 4) Set initial visuals
        text.alignment = TextAlignmentOptions.Center;
        text.text  = amount.ToString();
        text.color = isCrit ? critColor : normalColor;

        _startColor = text.color;
        _baseScale  = transform.localScale;

        // pop at start (scale ROOT)
        transform.localScale = _baseScale * Mathf.Max(1f, popScale);

        _valid = true;
        _age   = 0f;
    }

    void Update()
    {
        if (!_valid) return;

        _age += Time.deltaTime;

        // position derived from anchor only (no cumulative drift)
        float rise = Mathf.Max(0f, _age * riseSpeed);
        transform.position = _anchor + Vector3.up * rise;

        // face camera – rotation only
        var cam = Camera.main;
        if (billboardToCamera && cam)
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position, Vector3.up);

        // scale back to 1 over first quarter
        float tScale = Mathf.Clamp01(_age / (lifetime * 0.25f));
        transform.localScale = Vector3.Lerp(_baseScale * Mathf.Max(1f, popScale), _baseScale, tScale);

        // fade during second half
        float tFade = Mathf.InverseLerp(lifetime * 0.5f, lifetime, _age);
        var c = _startColor; c.a = Mathf.Lerp(1f, 0f, tFade);
        text.color = c;

        if (_age >= lifetime) Destroy(gameObject);
    }
}
