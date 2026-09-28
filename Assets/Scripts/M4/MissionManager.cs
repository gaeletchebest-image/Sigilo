using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class MissionManager : MonoBehaviour
{
    public enum RunState { Playing, Paused, Victory, Defeat }

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
    private RunState state = RunState.Playing;
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
        state = RunState.Playing;
        hasFolder = false;
        Time.timeScale = 1f;
        if (folder == null) folder = FindFirstObjectByType<MissionFolder>();
        if (exit == null) exit = FindFirstObjectByType<ServiceExit>();
    }

    private void Start()
    {
        CaptureCursor();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

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
        return true;
    }

    public void RegisterCapture()
    {
        if (state != RunState.Playing) return;
        state = RunState.Defeat;
        Time.timeScale = 0f;
        ReleaseCursor();
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
        GUI.Box(new Rect(Screen.width / scale * .5f - 185f, 12f, 370f, 42f), Objective);

        if (Time.unscaledTime < noticeUntil && state == RunState.Playing)
            GUI.Box(new Rect(Screen.width / scale * .5f - 150f, Screen.height / scale - 126f, 300f, 38f), notice);

        if (state == RunState.Paused || state == RunState.Victory || state == RunState.Defeat)
        {
            string title = state == RunState.Paused ? "PAUSA" :
                state == RunState.Victory ? "MISIÓN COMPLETADA" : "CAPTURADO";
            string detail = state == RunState.Paused ? "Esc — continuar" :
                state == RunState.Victory ? "Recuperaste la carpeta y escapaste." : "Un guardia te alcanzó.";
            float centerX = Screen.width / scale * .5f;
            float centerY = Screen.height / scale * .5f;
            GUI.Box(new Rect(centerX - 190f, centerY - 95f, 380f, 190f), title);
            GUI.Label(new Rect(centerX - 165f, centerY - 52f, 330f, 34f), detail);

            if (state == RunState.Paused)
            {
                if (GUI.Button(new Rect(centerX - 100f, centerY + 5f, 200f, 36f), "Reanudar"))
                    Resume();
            }
            else if (GUI.Button(new Rect(centerX - 100f, centerY + 5f, 200f, 36f), "Reintentar (R)"))
            {
                Retry();
            }
        }

        GUI.matrix = previousMatrix;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
