using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DamageOnHit : MonoBehaviour
{
    [Header("Settings")]
    public LayerMask enemyLayers;
    public bool oneHitPerEnable = true;

    PlayerStats _player;
    UpperBodyComboController _combo;   // NEW
    int _lastSwingIndex = 0;           // NEW

    bool _hasHitThisEnable;
    Collider _col;

    void Awake()
    {
        _player = GetComponentInParent<PlayerStats>();
        _combo  = GetComponentInParent<UpperBodyComboController>(); // NEW
        _col = GetComponent<Collider>();
        _col.isTrigger = true;
    }

    void OnEnable()
    {
        _hasHitThisEnable = false;
        if (_combo) _combo.OnAttackStarted += OnAttackStarted;       // NEW
    }

    void OnDisable()
    {
        if (_combo) _combo.OnAttackStarted -= OnAttackStarted;       // NEW
    }

    void OnAttackStarted(int step) => _lastSwingIndex = step;        // NEW

    void OnTriggerEnter(Collider other)
    {
        if (oneHitPerEnable && _hasHitThisEnable) return;
        if (!other.TryGetComponent(out EnemyStats enemy)) return;
        if ((enemyLayers.value & (1 << other.gameObject.layer)) == 0) return;

        int baseDmg  = Mathf.RoundToInt(_player ? _player.TotalDamage : 0f);
        float critPc = _player ? _player.TotalCritChancePct : 0f;
        bool isCrit  = critPc > 0f && Random.value < (critPc / 100f);
        int finalDmg = isCrit ? baseDmg * 2 : baseDmg;

        enemy.TakeDamage(finalDmg, source: gameObject);

        Vector3 hitPoint = other.ClosestPoint(_col.bounds.center) + Vector3.up * 0.25f;

        // floating number (your existing system)
        DamageNumbers.Show(hitPoint, finalDmg, isCrit);

        // 🔊 per-swing impact on the enemy
        var impact = enemy.GetComponent<ImpactSFX>();
        if (impact) impact.PlayHitVariant(hitPoint, isCrit, _lastSwingIndex);

        _hasHitThisEnable = true;
    }
}
