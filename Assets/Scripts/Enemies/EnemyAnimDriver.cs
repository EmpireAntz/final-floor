using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAnimDriver2D : MonoBehaviour
{
    public Animator anim; // assign in Inspector if Animator is on a child
    NavMeshAgent agent;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (!anim) anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (!anim) return;
        float max = Mathf.Max(agent.speed, 0.01f);
        Vector3 vLocal = transform.InverseTransformDirection(agent.velocity) / max;
        anim.SetFloat("MoveX", vLocal.x);
        anim.SetFloat("MoveZ", vLocal.z);
    }
}
