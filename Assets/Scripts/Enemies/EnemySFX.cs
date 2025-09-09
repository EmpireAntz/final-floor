using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class EnemySFX : MonoBehaviour
{
    [Header("SFX")]
    public AudioClip[] swingClips;   // slash
    public AudioClip[] hitClips;     // impact on player

    [Range(0f,1f)] public float volume = 0.9f;
    public Vector2 pitchJitter = new Vector2(0.95f, 1.05f);

    AudioSource _source;

    void Awake() {
        _source = GetComponent<AudioSource>();
        _source.spatialBlend = 1f; // 3D
    }

    void PlayRandom(AudioClip[] bank) {
        if (bank == null || bank.Length == 0) return;
        _source.pitch = Random.Range(pitchJitter.x, pitchJitter.y);
        _source.PlayOneShot(bank[Random.Range(0, bank.Length)], volume);
    }

    // Called from animation event in attack clip
    public void AnimEvent_SwingSFX() => PlayRandom(swingClips);

    // Called from attack script after damage lands
    public void PlayImpactSFX() => PlayRandom(hitClips);
}
