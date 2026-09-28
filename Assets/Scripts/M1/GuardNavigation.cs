using UnityEngine;
using UnityEngine.AI;

public sealed class GuardNavigation : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform[] waypoints;
    [SerializeField, Min(0.1f)] private float patrolSpeed = 2f;
    [SerializeField, Min(0.1f)] private float chaseSpeed = 3.5f;
    [SerializeField, Min(0f)] private float waypointPause = 1f;
    [SerializeField] private int waypoint;
    private float pauseUntil;

    public int WaypointIndex => waypoint;
    public bool IsStopped => agent == null || agent.isStopped;
    public bool AtDestination => agent != null && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + .1f;
    public Vector3 Position => transform.position;
    public Vector3 Destination => agent != null && agent.hasPath ? agent.destination : transform.position;

    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
    }

    public void BeginPatrol()
    {
        waypoint = 0;
        pauseUntil = 0f;
        agent.speed = patrolSpeed;
        agent.isStopped = false;
        if (waypoints.Length > 0) SetDestination(waypoints[0].position);
    }

    public void SetDestination(Vector3 destination)
    {
        if (agent == null || !agent.isOnNavMesh) return;
        agent.isStopped = false;
        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
    }

    public void SetChaseMode()
    {
        agent.speed = chaseSpeed;
        agent.isStopped = false;
    }

    public void SetSearchMode()
    {
        agent.speed = patrolSpeed;
        agent.isStopped = false;
    }

    public void TickPatrol()
    {
        if (waypoints == null || waypoints.Length == 0 || agent == null || !agent.isOnNavMesh) return;
        if (Time.time < pauseUntil) return;
        if (agent.isStopped)
        {
            agent.isStopped = false;
            SetDestination(waypoints[waypoint].position);
        }
        if (AtDestination)
        {
            waypoint = (waypoint + 1) % waypoints.Length;
            pauseUntil = Time.time + waypointPause;
            agent.isStopped = true;
        }
    }

    public void ResumeNearestWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        int best = 0;
        float distance = float.MaxValue;
        for (int i = 0; i < waypoints.Length; i++)
        {
            float d = Vector3.Distance(transform.position, waypoints[i].position);
            if (d < distance) { distance = d; best = i; }
        }
        waypoint = best;
        agent.speed = patrolSpeed;
        SetDestination(waypoints[waypoint].position);
    }

    private void OnDrawGizmosSelected()
    {
        if (waypoints == null) return;
        Gizmos.color = Color.green;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawSphere(waypoints[i].position, .16f);
            if (waypoints.Length > 1 && waypoints[(i + 1) % waypoints.Length] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[(i + 1) % waypoints.Length].position);
        }
        Gizmos.color = Color.blue;
        if (agent != null && agent.hasPath)
        {
            Vector3[] corners = agent.path.corners;
            for (int i = 1; i < corners.Length; i++) Gizmos.DrawLine(corners[i - 1], corners[i]);
            Gizmos.DrawSphere(agent.destination, .18f);
        }
    }
}
