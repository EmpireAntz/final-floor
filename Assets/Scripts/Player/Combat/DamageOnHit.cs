using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DamageOnHit : MonoBehaviour
{
    [Header("Settings")]
    public LayerMask enemyLayers;
    public bool oneHitPerEnable = true;

    PlayerStats _player;
    bool _hasHitThisEnable;
    Collider _col;

    void Awake()
    {
        _player = GetComponentInParent<PlayerStats>();
        _col = GetComponent<Collider>();
        _col.isTrigger = true;
    }

    void OnEnable() => _hasHitThisEnable = false;

    void OnTriggerEnter(Collider other)
    {
        if (oneHitPerEnable && _hasHitThisEnable) return;

        if (!other.TryGetComponent<EnemyStats>(out var enemy)) return;
        if ((enemyLayers.value & (1 << other.gameObject.layer)) == 0) return;

        // --- Damage calculation ---
        int baseDmg = Mathf.RoundToInt(_player ? _player.TotalDamage : 0f); // round first
        float critPct = _player ? _player.TotalCritChancePct : 0f;

        bool isCrit = critPct > 0f && Random.value < (critPct / 100f);
        int finalDmg = isCrit ? (baseDmg * 2) : baseDmg;

        if (isCrit)
            Debug.Log($"CRIT! x2 → {finalDmg}");

        enemy.TakeDamage(finalDmg, source: gameObject);
        _hasHitThisEnable = true;
    }
}
