using UnityEngine;
using System.Collections;

public class ScreenShake : MonoBehaviour
{
    public static ScreenShake Instance { get; private set; }

    [Header("Default Settings")]
    [Range(0f, 1f)] public float defaultIntensity = 0.2f;
    [Range(0f, 2f)] public float defaultDuration  = 0.2f;

    Vector3 _originalPos;
    Coroutine _shakeRoutine;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _originalPos = transform.localPosition;
    }

    /// <summary>
    /// Call this to trigger a shake.
    /// </summary>
    public void Shake(float intensity = -1f, float duration = -1f)
    {
        if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
        _shakeRoutine = StartCoroutine(DoShake(
            intensity < 0 ? defaultIntensity : intensity,
            duration  < 0 ? defaultDuration  : duration));
    }

    IEnumerator DoShake(float intensity, float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;
            transform.localPosition = _originalPos + new Vector3(x, y, 0f);
            yield return null;
        }

        transform.localPosition = _originalPos;
        _shakeRoutine = null;
    }
}
