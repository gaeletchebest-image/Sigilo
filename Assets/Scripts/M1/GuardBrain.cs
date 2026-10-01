using UnityEngine;

public sealed class GuardBrain : MonoBehaviour
{
    public enum State { Patrol, Investigate, Chase }

    [SerializeField] private GuardPerception perception;
    [SerializeField] private GuardSuspicion suspicion;
    [SerializeField] private GuardNavigation navigation;
    [SerializeField, Min(0.1f)] private float searchDuration = 4f;

    private Transform player;
    private PlayerStealthState playerStealth;
    [SerializeField] private State state;
    private float searchUntil;
    private bool investigatingSound;
    [SerializeField] private Vector3 lastKnown;
    private string gameMessage = "";
    private float messageUntil;
    private int lastSuspicionBand;
    private State lastAudioState;

    public float Suspicion => suspicion != null ? suspicion.Value : 0f;
    public State CurrentState => state;
    public Vector3 LastKnown => lastKnown;
    public int WaypointIndex => navigation != null ? navigation.WaypointIndex : 0;

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
        lastAudioState = state;
    }

    private void Update()
    {
        if (MissionManager.Instance != null && !MissionManager.Instance.IsPlaying) return;
        if (player == null || perception == null || suspicion == null || navigation == null) return;
        bool visible = perception.CanSeePlayer();
        bool near = perception.IsNear;

        if (visible || near) lastKnown = player.position;
        if (investigatingSound && (visible || near))
        {
            investigatingSound = false;
            state = State.Patrol;
            navigation.ResumeNearestWaypoint();
        }
        suspicion.Evaluate(visible, near, state == State.Chase);
        if (suspicion.Value >= 100f) state = State.Chase;

        int suspicionBand = suspicion.Value >= 70f ? 2 : suspicion.Value >= 20f ? 1 : 0;
        if (suspicionBand > lastSuspicionBand)
            ProceduralAudioFeedback.Instance?.PlaySuspicion();
        lastSuspicionBand = suspicionBand;

        if (state == State.Chase && lastAudioState != State.Chase)
            ProceduralAudioFeedback.Instance?.PlayChase();
        lastAudioState = state;

        if (state == State.Chase)
        {
            navigation.SetChaseMode();
            if (visible || near)
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
            else
            {
                state = State.Investigate;
                searchUntil = Time.time + searchDuration;
                navigation.SetSearchMode();
                navigation.SetDestination(lastKnown);
            }
        }
        else if (state == State.Investigate)
        {
            navigation.SetSearchMode();
            if (Time.time >= searchUntil && navigation.AtDestination)
            {
                investigatingSound = false;
                state = State.Patrol;
                suspicion.ResetValue();
                navigation.ResumeNearestWaypoint();
            }
        }
        else if (Suspicion > 0f && navigation.AtDestination)
        {
            state = State.Investigate;
            searchUntil = Time.time + searchDuration;
            navigation.SetDestination(lastKnown);
        }
        else
        {
            navigation.TickPatrol();
        }
    }

    public void SetProximity(bool value) => perception?.SetProximity(value);

    public bool InvestigateSound(Vector3 origin, float duration)
    {
        if (state == State.Chase || perception == null || perception.CanSeePlayer() || perception.IsNear)
            return false;

        state = State.Investigate;
        investigatingSound = true;
        searchUntil = Time.time + Mathf.Max(0.1f, duration);
        navigation.SetSearchMode();
        navigation.SetDestination(origin);
        return true;
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

