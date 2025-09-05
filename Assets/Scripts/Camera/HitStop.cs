using UnityEngine;
using System.Collections;

public class HitStop : MonoBehaviour
{
    public static HitStop Instance { get; private set; }

    [Header("Defaults")]
    [Range(0f, 1f)] public float defaultDuration = 0.05f; // 50 ms
    [Range(0f, 1f)] public float slowdownFactor = 0f;      // 0 = full pause

    bool _isStopping;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Stop(float duration = -1f)
    {
        if (!_isStopping) StartCoroutine(DoStop(duration < 0 ? defaultDuration : duration));
    }

    IEnumerator DoStop(float duration)
    {
        _isStopping = true;
        float oldScale = Time.timeScale;

        Time.timeScale = slowdownFactor;
        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = oldScale;
        _isStopping = false;
    }
}
