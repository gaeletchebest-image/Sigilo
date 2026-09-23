using UnityEngine;

public class GameController : MonoBehaviour
{
    
    public static GameController Instance;

    [SerializeField] ControlsController controls;
    [SerializeField] GameMotor gameMotor;
    [SerializeField] PlayerScr player;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else DestroyImmediate(this);
    }



    public ControlsController GetControlsController() => controls;
    public GameMotor GetGameMotor() => gameMotor;
    public PlayerScr GetPlayer() => player;


}
