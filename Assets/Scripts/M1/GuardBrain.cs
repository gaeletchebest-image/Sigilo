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

        float scale = Mathf.Max(1f, Mathf.Min(Screen.width / 960f, Screen.height / 540f));
        Matrix4x4 previousMatrix = GUI.matrix;
        int previousFont = GUI.skin.label.fontSize;
        int previousBoxFont = GUI.skin.box.fontSize;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        GUI.skin.label.fontSize = 18;
        GUI.skin.box.fontSize = 18;
        float screenWidth = Screen.width / scale;
        bool guardB = gameObject.name.IndexOf("B", System.StringComparison.OrdinalIgnoreCase) >= 0;
        const float panelWidth = 278f;
        float x = guardB ? screenWidth - 290f : 12f;
        string stateText = state == State.Patrol ? "PATRULLA" : state == State.Investigate ? "ALERTA" : "PERSECUCIÓN";
        Color stateColor = state == State.Patrol ? new Color(.12f, .34f, .25f) :
            state == State.Investigate ? new Color(.56f, .34f, .08f) : new Color(.55f, .12f, .12f);
        GUI.color = stateColor;
        GUI.Box(new Rect(x, 12, panelWidth, 116), GUIContent.none);
        GUI.color = Color.white;
        GUI.Label(new Rect(x + 12, 16, panelWidth - 24, 24), guardB ? "GUARDIA B" : "GUARDIA A");
        GUI.Label(new Rect(x + 12, 39, panelWidth - 24, 24), "Estado: " + stateText);
        GUI.Label(new Rect(x + 12, 63, panelWidth - 24, 24), "Sospecha: " + Mathf.RoundToInt(Suspicion) + "%");
        GUI.color = new Color(.08f, .09f, .11f);
        GUI.DrawTexture(new Rect(x + 12, 93, panelWidth - 24, 14), Texture2D.whiteTexture);
        GUI.color = state == State.Chase ? Color.red : state == State.Investigate ? new Color(1f, .65f, .12f) : new Color(.45f, .85f, .5f);
        GUI.DrawTexture(new Rect(x + 12, 93, (panelWidth - 24) * Suspicion / 100f, 14), Texture2D.whiteTexture);
        GUI.color = Color.white;
        if (Time.time < messageUntil)
            GUI.Box(new Rect(Screen.width / scale * .5f - 250, Screen.height / scale * .5f - 34, 500, 68), gameMessage);
        GUI.skin.label.fontSize = previousFont;
        GUI.skin.box.fontSize = previousBoxFont;
        GUI.matrix = previousMatrix;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position + Vector3.up * 1.5f, lastKnown);
        Gizmos.DrawSphere(lastKnown, .14f);
    }
}

