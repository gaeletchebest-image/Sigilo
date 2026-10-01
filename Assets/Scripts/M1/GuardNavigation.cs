using UnityEngine;
using UnityEngine.AI;

public sealed class GuardNavigation : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform[] waypoints;
    [SerializeField, Min(0.1f)] private float patrolSpeed = 1.9f;
    [SerializeField, Min(0.1f)] private float chaseSpeed = 3.5f;
    [SerializeField, Min(0f)] private float waypointPause = 1.1f;
    [SerializeField, Min(0.05f)] private float destinationRefreshInterval = 0.3f;
    [SerializeField, Min(0f)] private float destinationMoveThreshold = 0.65f;
    [SerializeField, Min(0.1f)] private float patrolRouteFailureTimeout = 1f;
    [SerializeField] private int waypoint;
    private float pauseUntil;
    private float patrolFailureSince = -1f;
    private float destinationRequestedAt = float.NegativeInfinity;
    private Vector3 requestedDestination;
    private bool hasRequestedDestination;

    public int WaypointIndex => waypoint;
    public bool IsStopped => agent == null || agent.isStopped;
    public bool AtDestination => agent != null && agent.isOnNavMesh &&
        agent.pathStatus == NavMeshPathStatus.PathComplete && !agent.pathPending &&
        agent.remainingDistance <= agent.stoppingDistance + .1f;
    public bool DestinationFailed => agent != null && agent.isOnNavMesh && hasRequestedDestination &&
        Time.time - destinationRequestedAt >= .75f && !agent.pathPending &&
        (!agent.hasPath || agent.pathStatus != NavMeshPathStatus.PathComplete);
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
        patrolFailureSince = -1f;
        hasRequestedDestination = false;
        if (agent == null) return;
        agent.speed = patrolSpeed;
        agent.isStopped = false;
        if (waypoints != null && waypoints.Length > 0 && waypoints[0] != null)
            SetDestination(waypoints[0].position);
    }

    public bool SetDestination(Vector3 destination)
    {
        if (agent == null || !agent.isOnNavMesh) return false;
        if (hasRequestedDestination &&
            (destination - requestedDestination).sqrMagnitude < destinationMoveThreshold * destinationMoveThreshold &&
            Time.time - destinationRequestedAt < destinationRefreshInterval)
            return true;

        agent.isStopped = false;
        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            if (agent.SetDestination(hit.position))
            {
                requestedDestination = hit.position;
                destinationRequestedAt = Time.time;
                hasRequestedDestination = true;
                return true;
            }
        }
        hasRequestedDestination = false;
        return false;
    }

    public void StopAtDestination()
    {
        if (agent != null && agent.isOnNavMesh && AtDestination)
            agent.isStopped = true;
    }

    public void SetChaseMode()
    {
        if (agent == null) return;
        agent.speed = chaseSpeed;
        agent.isStopped = false;
    }

    public void SetSearchMode()
    {
        if (agent == null) return;
        agent.speed = patrolSpeed;
        agent.isStopped = false;
    }

    public void TickPatrol()
    {
        if (waypoints == null || waypoints.Length == 0 || agent == null || !agent.isOnNavMesh) return;
        waypoint = Mathf.Clamp(waypoint, 0, waypoints.Length - 1);
        if (Time.time < pauseUntil) return;
        if (waypoints[waypoint] == null)
        {
            AdvanceWaypoint();
            return;
        }
        if (agent.isStopped)
        {
            agent.isStopped = false;
        }
        else if (DestinationFailed)
        {
            if (patrolFailureSince < 0f) patrolFailureSince = Time.time;
            if (Time.time - patrolFailureSince >= patrolRouteFailureTimeout)
            {
                patrolFailureSince = -1f;
                AdvanceWaypoint();
                pauseUntil = Time.time;
                return;
            }
        }
        else if (!hasRequestedDestination)
        {
            if (SetDestination(waypoints[waypoint].position))
                patrolFailureSince = -1f;
            else
            {
                if (patrolFailureSince < 0f) patrolFailureSince = Time.time;
                if (Time.time - patrolFailureSince >= patrolRouteFailureTimeout)
                {
                    patrolFailureSince = -1f;
                    AdvanceWaypoint();
                    pauseUntil = Time.time;
                    return;
                }
            }
        }
        else patrolFailureSince = -1f;
        if (AtDestination)
        {
            AdvanceWaypoint();
        }
    }

    private void AdvanceWaypoint()
    {
        waypoint = (waypoint + 1) % waypoints.Length;
        pauseUntil = Time.time + waypointPause;
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
        hasRequestedDestination = false;
    }

    public void ResumeNearestWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0 || agent == null) return;
        int best = 0;
        float distance = float.MaxValue;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            float d = Vector3.Distance(transform.position, waypoints[i].position);
            if (d < distance) { distance = d; best = i; }
        }
        if (distance == float.MaxValue) return;
        waypoint = best;
        agent.speed = patrolSpeed;
        hasRequestedDestination = false;
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
