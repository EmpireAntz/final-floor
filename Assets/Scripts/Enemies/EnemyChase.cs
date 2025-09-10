using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyChase : MonoBehaviour
{
    [Header("Targets")]
    public Transform player;                 // assign or auto-find

    [Header("Chase")]
    public float chaseRange = 10f;

    [Header("Vision")]
    public Transform eye;                    // optional: head/eye transform; else uses root
    public LayerMask visionBlockers;         // set to Ground/Walls/Props (NOT Player/Enemy)
    [Range(0f, 360f)] public float fovDegrees = 160f; // set 360 to ignore FOV

    NavMeshAgent agent;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (!player) player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (!eye)    eye    = transform; // fallback
    }

    void Update()
    {
        if (!player) return;

        float dist = Vector3.Distance(transform.position, player.position);
        bool inRange = dist <= chaseRange;
        bool canSee  = inRange && HasLineOfSight();

        if (canSee)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
        else
        {
            agent.isStopped = true; // idle (or keep last known pos if you want)
        }
    }

    bool HasLineOfSight()
    {
        // Optional FOV check
        if (fovDegrees > 0f && fovDegrees < 360f)
        {
            Vector3 toPlayerFlat = player.position - transform.position;
            toPlayerFlat.y = 0f;
            if (toPlayerFlat.sqrMagnitude > 0.001f)
            {
                float ang = Vector3.Angle(transform.forward, toPlayerFlat);
                if (ang > fovDegrees * 0.5f) return false;
            }
        }

        Vector3 origin = (eye ? eye.position : transform.position + Vector3.up * 1.5f);
        Vector3 target = player.position + Vector3.up * 1.0f; // aim roughly at chest
        Vector3 dir    = target - origin;
        float  len     = dir.magnitude;

        // If the ray hits any "visionBlockers" before the player -> vision is blocked
        return !Physics.Raycast(origin, dir.normalized, len, visionBlockers, QueryTriggerInteraction.Ignore);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // simple gizmo to visualize the LOS ray
        if (player)
        {
            Gizmos.color = Color.red;
            Vector3 o = eye ? eye.position : transform.position + Vector3.up * 1.5f;
            Gizmos.DrawLine(o, player.position + Vector3.up);
        }
    }
#endif
}
