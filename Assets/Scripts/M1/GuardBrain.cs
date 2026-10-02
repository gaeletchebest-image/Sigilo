using UnityEngine;

public sealed class GuardBrain : MonoBehaviour
{
    // Keep the existing numeric values stable because state is serialized in scenes.
    public enum State { Patrol, Investigate, Chase, Search }

    [SerializeField] private GuardPerception perception;
    [SerializeField] private GuardSuspicion suspicion;
    [SerializeField] private GuardNavigation navigation;
    [SerializeField, Min(0.1f)] private float searchDuration = 4f;
    [SerializeField, Min(0f)] private float chaseMemoryDuration = 1.25f;
    [SerializeField, Min(0.1f)] private float routeFailureTimeout = 1.5f;
    [SerializeField, Min(0.1f)] private float soundAttentionDuration = 3f;

    private Transform player;
    private PlayerStealthState playerStealth;
    [SerializeField] private State state;
    private float searchUntil;
    private float lastContactAt = float.NegativeInfinity;
    private float investigationDuration;
    private float routeFailureSince = -1f;
    [SerializeField] private Vector3 lastKnown;
    private string gameMessage = "";
    private float messageUntil;
    private int lastSuspicionBand;
    private State lastAudioState;
    private float soundAttentionUntil;

    public float Suspicion => suspicion != null ? suspicion.Value : 0f;
    public State CurrentState => state;
    public Vector3 LastKnown => lastKnown;
    public int WaypointIndex => navigation != null ? navigation.WaypointIndex : 0;
    public bool IsSoundAlerted => Time.time < soundAttentionUntil;

    private void Awake()
    {
        if (perception == null) perception = GetComponent<GuardPerception>();
        if (suspicion == null) suspicion = GetComponent<GuardSuspicion>();
        if (navigation == null) navigation = GetComponent<GuardNavigation>();
    }

    private void Start()
    {
        GameObject found = GameObject.Find("Player");
        if (found != null)
        {
            player = found.transform;
            playerStealth = found.GetComponent<PlayerStealthState>();
        }
        if (perception != null) perception.Initialize(player);
        if (navigation != null) navigation.BeginPatrol();
        investigationDuration = searchDuration;
        lastAudioState = state;
    }

    private void Update()
    {
        if (MissionManager.Instance != null &&
            (!MissionManager.Instance.IsPlaying || MissionManager.Instance.IsCinematic)) return;
        if (player == null || perception == null || suspicion == null || navigation == null) return;
        bool visible = perception.CanSeePlayer();
        bool near = perception.IsNear;

        bool hasContact = visible || near;
        if (hasContact)
        {
            lastKnown = player.position;
            lastContactAt = Time.time;
        }

        suspicion.Evaluate(visible, near, state == State.Chase);
        if (suspicion.Value >= 100f) state = State.Chase;

        int suspicionBand = suspicion.Value >= 70f ? 2 : suspicion.Value >= 20f ? 1 : 0;
        if (suspicionBand < lastSuspicionBand)
            lastSuspicionBand = suspicionBand;
        if (suspicionBand > lastSuspicionBand)
        {
            ProceduralAudioFeedback.Instance?.PlaySuspicion();
            lastSuspicionBand = suspicionBand;
        }

        if (state == State.Chase && lastAudioState != State.Chase)
            ProceduralAudioFeedback.Instance?.PlayChase();
        lastAudioState = state;

        if (state == State.Chase)
        {
            navigation.SetChaseMode();
            if (hasContact)
            {
                navigation.SetDestination(lastKnown);
                if ((playerStealth == null || !playerStealth.IsHidden) &&
                    Vector3.Distance(transform.position, player.position) <= 1.15f)
                {
                    if (MissionManager.Instance != null)
                        MissionManager.Instance.RegisterCapture();
                    else
                    {
                        gameMessage = "CAPTURADO — reiniciá la escena para volver a intentar";
                        messageUntil = Time.time + 1000f;
                        Time.timeScale = 0f;
                    }
                }
            }
            else if (Time.time - lastContactAt > chaseMemoryDuration)
            {
                BeginInvestigation(searchDuration);
            }
            else navigation.SetDestination(lastKnown);
            return;
        }

        // Partial suspicion now sends the guard to the clue instead of making it
        // finish its unrelated patrol route first.
        if (hasContact && state != State.Chase)
        {
            if (state != State.Investigate)
                BeginInvestigation(searchDuration);
            navigation.SetSearchMode();
            navigation.SetDestination(lastKnown);
        }
        else if (state == State.Investigate)
        {
            navigation.SetSearchMode();
            if (navigation.DestinationFailed)
            {
                if (routeFailureSince < 0f) routeFailureSince = Time.time;
                if (Time.time - routeFailureSince >= routeFailureTimeout)
                {
                    FinishInvestigation();
                    return;
                }
            }
            else routeFailureSince = -1f;

            if (navigation.AtDestination)
            {
                state = State.Search;
                searchUntil = Time.time + investigationDuration;
                navigation.StopAtDestination();
            }
        }
        else if (state == State.Search)
        {
            navigation.StopAtDestination();
            if (Time.time >= searchUntil)
                FinishInvestigation();
        }
        else navigation.TickPatrol();
    }

    public void SetProximity(bool value) => perception?.SetProximity(value);

    public bool InvestigateSound(Vector3 origin, float duration)
    {
        if (state == State.Chase || perception == null || perception.CanSeePlayer() || perception.IsNear)
            return false;

        if (!navigation.SetDestination(origin)) return false;
        lastKnown = origin;
        investigationDuration = Mathf.Max(0.1f, duration);
        routeFailureSince = -1f;
        soundAttentionUntil = Time.time + soundAttentionDuration;
        state = State.Investigate;
        navigation.SetSearchMode();
        return true;
    }

    private void BeginInvestigation(float duration)
    {
        state = State.Investigate;
        investigationDuration = Mathf.Max(0.1f, duration);
        routeFailureSince = -1f;
        navigation.SetSearchMode();
        if (!navigation.SetDestination(lastKnown))
            FinishInvestigation();
    }

    private void FinishInvestigation()
    {
        routeFailureSince = -1f;
        suspicion.ResetValue();
        lastSuspicionBand = 0;
        state = State.Patrol;
        navigation.ResumeNearestWaypoint();
    }

    private void OnGUI()
    {
        if (MissionManager.Instance != null && MissionManager.Instance.State == MissionManager.RunState.StartMenu)
            return;
        if (Time.time < messageUntil)
            GUI.Box(new Rect(Screen.width * .5f - 250f, Screen.height * .5f - 34f, 500f, 68f), gameMessage);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position + Vector3.up * 1.5f, lastKnown);
        Gizmos.DrawSphere(lastKnown, .14f);
    }
}

