using UnityEngine;

[System.Serializable]
public struct VariantClips
{
    public AudioClip[] hits;   // normal impact pool for this swing
    public AudioClip[] crits;  // optional crit pool for this swing
}

public class ImpactSFX : MonoBehaviour
{
    [Header("Per-swing clip sets (index = swing)")]
    public VariantClips[] variants;       // size = number of swings (e.g., 3)

    [Header("Mix")]
    [Range(0f,1f)] public float volume = 0.9f;
    public bool usePitchJitter = true;
    [Range(0.5f, 2f)] public float pitchMin = 0.95f;
    [Range(0.5f, 2f)] public float pitchMax = 1.05f;

    [Header("3D Settings")]
    [Range(0f,1f)] public float spatialBlend = 1f;
    public float minDistance = 1f;
    public float maxDistance = 20f;

    public void PlayHitVariant(Vector3 worldPos, bool isCrit, int swingIndex)
    {
        if (variants == null || variants.Length == 0) return;

        var v = variants[Mathf.Clamp(swingIndex, 0, variants.Length - 1)];
        AudioClip clip = PickClip(isCrit && v.crits != null && v.crits.Length > 0 ? v.crits : v.hits);
        if (!clip) return;

        var go = new GameObject("ImpactSFX (one-shot)");
        go.transform.position = worldPos;

        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volume;
        src.spatialBlend = spatialBlend;
        src.minDistance = minDistance;
        src.maxDistance = maxDistance;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.playOnAwake = false;
        src.pitch = usePitchJitter ? Random.Range(pitchMin, pitchMax) : 1f;

        src.Play();
        Destroy(go, clip.length / Mathf.Max(0.01f, src.pitch));
    }

    AudioClip PickClip(AudioClip[] pool)
    {
        if (pool == null || pool.Length == 0) return null;
        return pool.Length == 1 ? pool[0] : pool[Random.Range(0, pool.Length)];
    }
}
