using UnityEngine;
using UnityEngine.AI;

public class EnemyDeath : MonoBehaviour
{
    public Animator anim;                 // assign if Animator is on a child
    public string deathTrigger = "Die";   // Animator trigger name
    public float destroyDelay = 3f;

    bool _dead;
    NavMeshAgent _agent;
    Collider[] _cols;
    MonoBehaviour[] _aiScripts;           // chase/attack/etc
    Rigidbody _rb;

    void Awake()
    {
        if (!anim) anim = GetComponentInChildren<Animator>();
        _agent = GetComponent<NavMeshAgent>();
        _rb    = GetComponent<Rigidbody>();
        _cols  = GetComponentsInChildren<Collider>(true);

        // collect common AI scripts to disable on death (add your own types if needed)
        _aiScripts = new MonoBehaviour[]
        {
            GetComponent<EnemyChase>(),
            GetComponent<EnemyMeleeAttack>(),
            GetComponent<EnemyAnimDriver>(),    
        };
    }

    public void Die()
    {
        if (_dead) return;
        _dead = true;

        // stop movement/AI
        if (_agent) { _agent.ResetPath(); _agent.isStopped = true; }
        foreach (var s in _aiScripts) if (s) s.enabled = false;

        // disable collisions so corpse doesn’t push anything
        foreach (var c in _cols) c.enabled = false;
        if (_rb) { _rb.isKinematic = true; _rb.useGravity = false; }

        // play animation
        if (anim && !string.IsNullOrEmpty(deathTrigger))
            anim.SetTrigger(deathTrigger);

        // destroy after delay
        Destroy(gameObject, destroyDelay);
    }
}
