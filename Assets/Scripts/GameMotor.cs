using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameMotor : MonoBehaviour
{

    [SerializeField] List<CheckPoint> checkPoints;
    [SerializeField] int checkpointIndex = 0;

    [SerializeField] int collectables = 0;

    [SerializeField] TMP_Text countCollectables;

    [SerializeField] List<ActiveOrDesactiveThings> sysThings;

    PlayerScr player;

    private void Start()
    {
        player = GameController.Instance.GetPlayer();
        foreach (ActiveOrDesactiveThings ac in sysThings) ac.Start();
    }

    private void Update()
    {
        
    }

    public bool actCheckpoint(CheckPoint check)
    {
        int index = checkPoints.FindIndex(x => x == check);
        if (index > checkpointIndex)
        {
            checkpointIndex = index;
            return true;
        }
        else return false;
    }

    /*void PlayerDied()
    {
        player.ResetPlayer(checkPoints[checkpointIndex].GetPosSpawn(), checkPoints[checkpointIndex].GetRotationSpawn());
    }*/

    public void AddCollectable() 
    {
        collectables++;
        countCollectables.text = $"{collectables} X";
    }

}

[System.Serializable]
class ActiveOrDesactiveThings
{
    public TriggerDetect Td;
    public List<GameObject> Things;
    public bool Active = true;

    public void Start() { Td.OnDetect += (object o, EventArgs e) => { foreach (GameObject t in Things) t.SetActive(Active); }; }

}
