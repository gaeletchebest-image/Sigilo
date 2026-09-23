using UnityEngine;
using UnityEngine.InputSystem;

public class ControlsController : MonoBehaviour
{

    InputSystem_Actions isa;

    [SerializeField] Vector2 _move;
    public Vector2 Move
    {
        get { return _move; }
        private set { _move = value; }
    }

    /*[SerializeField] bool _jump;
    public bool Jump
    {
        get { return _jump; }
        private set { _jump = value; }
    }*/

    [SerializeField] Vector2 _mouseDelta;
    public Vector2 MouseDelta
    {
        get { return _mouseDelta; }
        private set { _mouseDelta = value; }
    }

    private void Awake() => isa = new InputSystem_Actions();
    private void OnEnable() => isa.Enable();
    private void OnDisable() => isa.Disable();

    private void Update()
    {
        Move = isa.Player.Move.ReadValue<Vector2>();
        //Jump = isa.Player.Jump.IsPressed();

        MouseDelta = Mouse.current.delta.ReadValue();
    }

}
