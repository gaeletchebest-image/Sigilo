using UnityEngine;
using System;

public class TriggerDetect : MonoBehaviour
{

    public string tagDetect;

    [SerializeField] bool isActive = false;
    public EventHandler OnDetect;

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == tagDetect)
        {
            OnDetect?.Invoke(this, EventArgs.Empty);
            isActive = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == tagDetect) isActive = false;
    }

}