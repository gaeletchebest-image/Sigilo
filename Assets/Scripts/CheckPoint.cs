using System;
using UnityEngine;

public class CheckPoint : MonoBehaviour
{

    [SerializeField] TriggerDetect td;
    [SerializeField] MeshRenderer meshRender;
    [SerializeField] Transform posSpawn;

    GameMotor gm;

    private void Start()
    {
        gm = GameController.Instance.GetGameMotor();

        td.OnDetect += (object o, EventArgs e) => enterCheckpoint();
    }

    void enterCheckpoint()
    {
        if (gm.actCheckpoint(this))
        {
            meshRender.enabled = false;
            td.enabled = false;
        }
    }

    public Vector3 GetPosSpawn() => posSpawn.position;
    public Quaternion GetRotationSpawn() => posSpawn.localRotation;

}
