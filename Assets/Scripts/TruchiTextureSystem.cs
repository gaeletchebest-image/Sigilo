using UnityEngine;
using System.Collections.Generic;

public class TruchiTextureSystem : MonoBehaviour
{

    [SerializeField] List<MeshRenderer> groundsRenderers;
    [SerializeField] List<MeshRenderer> wallsRenderers;

    private void Start()
    {
        foreach (MeshRenderer ren in groundsRenderers)
        {
            ren.material.mainTextureScale = new Vector3(ren.transform.localScale.x / 10, ren.transform.localScale.z / 10);
        }

        foreach(MeshRenderer ren in wallsRenderers)
        {
            ren.material.mainTextureScale = new Vector3(ren.transform.localScale.x / 5, ren.transform.localScale.y / 3);
        }
    }

}
