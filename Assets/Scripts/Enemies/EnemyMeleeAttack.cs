using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMeleeAttack : MonoBehaviour
{
    public Transform player;               // assign or auto-find
    public Animator anim;                  // assign if Animator on a child
    public Transform attackPoint;          // blade/tip
    public float attackRange = 1.6f;       // ~= agent.stoppingDistance
    public float cooldown = 1.0f;          // time between attacks
    public float lockTime = 0.6f;          // how long to stand still during attack
    public float hitRadius = 0.6f;
    public int damage = 10;
    public LayerMask playerMask;           // Player layer
    public string attackTrigger = "Attack";

    NavMeshAgent agent;
    float nextTime, unlockAt;
    bool isAttacking;

    void Awake() {
        agent = GetComponent<NavMeshAgent>();
        if (!player) player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (!anim)   anim   = GetComponentInChildren<Animator>();
        agent.stoppingDistance = Mathf.Max(agent.stoppingDistance, attackRange - 0.1f);
    }

    void Update() {
        if (!player) return;

        float d = Vector3.Distance(transform.position, player.position);

        // keep facing player when close
        if (d <= attackRange) {
            Vector3 dir = player.position - transform.position; dir.y = 0;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 12f);
        }

        // lock movement during the attack
        if (isAttacking) {
            agent.isStopped = true;            // freeze while swinging
            if (Time.time >= unlockAt) {
                isAttacking = false;
                agent.isStopped = false;       // resume locomotion
            }
            return; // don't start new attacks while locked
        }

        // not attacking: only swing if within range and off cooldown
        if (d <= attackRange && Time.time >= nextTime) {
            StartAttack();
        }
        // else: chase script can keep moving normally
    }

    void StartAttack() {
        isAttacking = true;
        unlockAt = Time.time + lockTime;
        nextTime = Time.time + cooldown;
        agent.ResetPath();     // hard stop to avoid sliding
        agent.isStopped = true;
        if (anim) anim.SetTrigger(attackTrigger); else DealDamage(); // fallback
    }

    // Call this from the attack animation event at impact frame
    public void DealDamage()
    {
        var p = attackPoint ? attackPoint.position : transform.position;
        bool didHit = false;

        foreach (var c in Physics.OverlapSphere(p, hitRadius, playerMask))
        {
            if (c.TryGetComponent(out PlayerStats ps)) {
                ps.TakeDamage(damage);
                didHit = true;
                break;
            }
            var ps2 = c.GetComponentInParent<PlayerStats>();
            if (ps2) {
                ps2.TakeDamage(damage);
                didHit = true;
                break;
            }
        }

        if (didHit) {
            var sfx = GetComponent<EnemySFX>();
            if (sfx) sfx.PlayImpactSFX();
        }
    }


    // Optional: call this via an Animation Event at the END of the clip to time unlock precisely
    public void AnimEvent_EndAttack() {
        isAttacking = false;
        agent.isStopped = false;
    }

    void OnDrawGizmosSelected() {
        if (attackPoint) { Gizmos.color = Color.red; Gizmos.DrawWireSphere(attackPoint.position, hitRadius); }
    }
}
