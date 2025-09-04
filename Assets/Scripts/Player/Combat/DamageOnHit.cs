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
        if (!other.TryGetComponent(out EnemyStats enemy)) return;
        if ((enemyLayers.value & (1 << other.gameObject.layer)) == 0) return;

        // exact x2 crit off rounded base
        int baseDmg  = Mathf.RoundToInt(_player ? _player.TotalDamage : 0f);
        float critPc = _player ? _player.TotalCritChancePct : 0f;
        bool isCrit  = critPc > 0f && Random.value < (critPc / 100f);
        int finalDmg = isCrit ? baseDmg * 2 : baseDmg;

        if (isCrit) Debug.Log($"CRIT! x2 → {finalDmg}");

        enemy.TakeDamage(finalDmg, source: gameObject);

        // hit point = enemy surface closest to your slash collider
        Vector3 hitPoint = other.ClosestPoint(_col.bounds.center) + Vector3.up * 0.25f;
        DamageNumbers.Show(hitPoint, finalDmg, isCrit);

        _hasHitThisEnable = true;
    }
}
