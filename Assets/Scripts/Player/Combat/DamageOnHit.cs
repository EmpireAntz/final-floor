using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DamageOnHit : MonoBehaviour
{
    [Header("Settings")]
    public LayerMask enemyLayers;
    public bool oneHitPerEnable = true;

    [Header("FX")]
    public GameObject bloodPrefab;      // <-- assign FX_BloodSplatter prefab
    public float bloodAutoDestroy = 3f; // fallback if the particle Stop Action != Destroy
    public bool randomizeAroundNormal = true;

    PlayerStats _player;
    UpperBodyComboController _combo;  
    int _lastSwingIndex = 0;           

    bool _hasHitThisEnable;
    Collider _col;

    void Awake()
    {
        _player = GetComponentInParent<PlayerStats>();
        _combo  = GetComponentInParent<UpperBodyComboController>(); 
        _col = GetComponent<Collider>();
        _col.isTrigger = true;
    }

    void OnEnable()
    {
        _hasHitThisEnable = false;
        if (_combo) _combo.OnAttackStarted += OnAttackStarted;       
    }

    void OnDisable()
    {
        if (_combo) _combo.OnAttackStarted -= OnAttackStarted;      
    }

    void OnAttackStarted(int step) => _lastSwingIndex = step;       

    void OnTriggerEnter(Collider other)
    {
        if (oneHitPerEnable && _hasHitThisEnable) return;
        if (!other.TryGetComponent(out EnemyStats enemy)) return;
        if ((enemyLayers.value & (1 << other.gameObject.layer)) == 0) return;

        if (ScreenShake.Instance) ScreenShake.Instance.Shake();

        int baseDmg  = Mathf.RoundToInt(_player ? _player.TotalDamage : 0f);
        float critPc = _player ? _player.TotalCritChancePct : 0f;
        bool isCrit  = critPc > 0f && Random.value < (critPc / 100f);
        int finalDmg = isCrit ? baseDmg * 2 : baseDmg;

        enemy.TakeDamage(finalDmg, source: gameObject);

        // --- hit point & normal ------------------------------------------------
        Vector3 approxPoint = other.ClosestPoint(_col.bounds.center);
        Vector3 dir = (approxPoint - _col.bounds.center);
        if (dir.sqrMagnitude < 0.0001f) dir = other.transform.position - _col.bounds.center;
        dir.Normalize();

        Vector3 hitPoint = approxPoint;
        Vector3 hitNormal = -dir; // fallback

        // If we can, raycast to get a real contact point/normal on the enemy collider
        if (Physics.Raycast(_col.bounds.center, dir, out RaycastHit rh, 3f,
                            1 << other.gameObject.layer, QueryTriggerInteraction.Ignore))
        {
            hitPoint  = rh.point;
            hitNormal = rh.normal;
        }
        else
        {
            // lift slightly to avoid Z-fighting with floors
            hitPoint += Vector3.up * 0.1f;
        }

        // --- floating number ---------------------------------------------------
        DamageNumbers.Show(hitPoint, finalDmg, isCrit);

        // --- impact SFX per swing ----------------------------------------------
        var impact = enemy.GetComponent<ImpactSFX>();
        if (impact) impact.PlayHitVariant(hitPoint, isCrit, _lastSwingIndex);

        // --- flash the enemy (your bright red flash script) --------------------
        var flash = enemy.GetComponentInChildren<EnemyFlashMulti>();
        if (flash) flash.Flash();

        // --- spawn blood -------------------------------------------------------
        SpawnBlood(hitPoint, hitNormal);

        _hasHitThisEnable = true;
    }

    void SpawnBlood(Vector3 pos, Vector3 normal)
    {
        if (!bloodPrefab) return;

        // Face out from surface; optionally randomize around the normal for variety
        Quaternion rot = Quaternion.LookRotation(normal);
        if (randomizeAroundNormal)
            rot = Quaternion.AngleAxis(Random.Range(0f, 360f), normal) * rot;

        var go = Instantiate(bloodPrefab, pos, rot);

        // If the ParticleSystem doesn’t have Stop Action = Destroy, clean it up:
        if (bloodAutoDestroy > 0f)
            Destroy(go, bloodAutoDestroy);
    }
}
