using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class MissionManager : MonoBehaviour
{
    public enum RunState { StartMenu, Playing, Paused, Victory, Defeat }

    private static MissionManager instance;
    public static MissionManager Instance
    {
        get
        {
            if (instance == null) instance = FindFirstObjectByType<MissionManager>();
            return instance;
        }
    }

    [SerializeField] private MissionFolder folder;
    [SerializeField] private ServiceExit exit;
    private RunState state = RunState.StartMenu;
    private bool hasFolder;
    private int folderCollectionCount;
    private string notice = "";
    private float noticeUntil;

    public RunState State => state;
    public bool IsPlaying => state == RunState.Playing;
    public bool HasFolder => hasFolder;
    public int FolderCollectionCount => folderCollectionCount;
    public string Objective => hasFolder ? "Objetivo: Llegá a la salida" : "Objetivo: Recuperá la carpeta";

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        state = RunState.StartMenu;
        hasFolder = false;
        Time.timeScale = 0f;
        ReleaseCursor();
        if (folder == null) folder = FindFirstObjectByType<MissionFolder>();
        if (exit == null) exit = FindFirstObjectByType<ServiceExit>();
    }

    private void Start()
    {
        ReleaseCursor();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (state == RunState.StartMenu)
        {
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) BeginGame();
            return;
        }

        if (state == RunState.Paused)
        {
            if (keyboard.escapeKey.wasPressedThisFrame) Resume();
            return;
        }

        if (state == RunState.Playing)
        {
            if (keyboard.escapeKey.wasPressedThisFrame) Pause();
            return;
        }

        if (keyboard.rKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)
            Retry();
    }

    public bool RegisterFolderCollected()
    {
        if (state != RunState.Playing || hasFolder) return false;
        hasFolder = true;
        folderCollectionCount++;
        ShowNotice("Carpeta recuperada");
        ProceduralAudioFeedback.Instance?.PlayFolder();
        return true;
    }

    public bool TryEscape()
    {
        if (state != RunState.Playing) return false;
        if (!hasFolder)
        {
            ShowNotice("Falta la carpeta");
            return false;
        }

        state = RunState.Victory;
        Time.timeScale = 0f;
        ReleaseCursor();
        ProceduralAudioFeedback.Instance?.PlayVictory();
        return true;
    }

    public void RegisterCapture()
    {
        if (state != RunState.Playing) return;
        state = RunState.Defeat;
        Time.timeScale = 0f;
        ReleaseCursor();
        ProceduralAudioFeedback.Instance?.PlayDefeat();
    }

    public void BeginGame()
    {
        if (state != RunState.StartMenu) return;
        state = RunState.Playing;
        Time.timeScale = 1f;
        CaptureCursor();
    }

    public void Pause()
    {
        if (state != RunState.Playing) return;
        state = RunState.Paused;
        Time.timeScale = 0f;
        ReleaseCursor();
    }

    public void Resume()
    {
        if (state != RunState.Paused) return;
        state = RunState.Playing;
        Time.timeScale = 1f;
        CaptureCursor();
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.name);
    }

    public void ShowMissingFolder()
    {
        if (state == RunState.Playing) ShowNotice("Falta la carpeta");
    }

    private void ShowNotice(string message)
    {
        notice = message;
        noticeUntil = Time.unscaledTime + 2.5f;
    }

    private static void CaptureCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private static void ReleaseCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnGUI()
    {
        float scale = Mathf.Max(1f, Mathf.Min(Screen.width / 960f, Screen.height / 540f));
        Matrix4x4 previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        int previousLabelFont = GUI.skin.label.fontSize;
        int previousBoxFont = GUI.skin.box.fontSize;
        int previousButtonFont = GUI.skin.button.fontSize;
        GUI.skin.label.fontSize = 20;
        GUI.skin.box.fontSize = 18;
        GUI.skin.button.fontSize = 18;
        float centerX = Screen.width / scale * .5f;
        float centerY = Screen.height / scale * .5f;

        if (state == RunState.StartMenu)
        {
            GUI.Box(new Rect(centerX - 230f, centerY - 165f, 460f, 330f), GUIContent.none);
            GUI.Label(new Rect(centerX - 205f, centerY - 135f, 410f, 52f), "EL ÚLTIMO TURNO");
            GUI.Label(new Rect(centerX - 190f, centerY - 75f, 380f, 90f),
                "WASD mover  •  Mouse mirar\nE interactuar  •  Esc pausar");
            if (GUI.Button(new Rect(centerX - 100f, centerY + 40f, 200f, 48f), "Jugar")) BeginGame();
            GUI.Label(new Rect(centerX - 150f, centerY + 100f, 300f, 32f), "Recuperá la carpeta y escapá");
            GUI.skin.label.fontSize = previousLabelFont;
            GUI.skin.box.fontSize = previousBoxFont;
            GUI.skin.button.fontSize = previousButtonFont;
            GUI.matrix = previousMatrix;
            return;
        }

        GUI.Box(new Rect(centerX - 185f, 12f, 370f, 42f), Objective);

        if (Time.unscaledTime < noticeUntil && state == RunState.Playing)
            GUI.Box(new Rect(Screen.width / scale * .5f - 150f, Screen.height / scale - 126f, 300f, 38f), notice);

        if (state == RunState.Paused || state == RunState.Victory || state == RunState.Defeat)
        {
            string title = state == RunState.Paused ? "PAUSA" :
                state == RunState.Victory ? "MISIÓN COMPLETADA" : "CAPTURADO";
            string detail = state == RunState.Paused ? "Esc — continuar" :
                state == RunState.Victory ? "Recuperaste la carpeta y escapaste." : "Un guardia te alcanzó.";
            GUI.Box(new Rect(centerX - 190f, centerY - 95f, 380f, 190f), title);
            GUI.Label(new Rect(centerX - 165f, centerY - 58f, 330f, 34f), detail);

            if (state == RunState.Paused)
            {
                if (GUI.Button(new Rect(centerX - 100f, centerY - 10f, 200f, 36f), "Reanudar")) Resume();
                if (GUI.Button(new Rect(centerX - 100f, centerY + 36f, 200f, 36f), "Reiniciar (R)")) Retry();
            }
            else if (GUI.Button(new Rect(centerX - 100f, centerY + 5f, 200f, 36f), "Reintentar (R)"))
            {
                Retry();
            }
        }

        GUI.skin.label.fontSize = previousLabelFont;
        GUI.skin.box.fontSize = previousBoxFont;
        GUI.skin.button.fontSize = previousButtonFont;
        GUI.matrix = previousMatrix;
    }

    public void ReturnToStartMenu() => Retry();

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
