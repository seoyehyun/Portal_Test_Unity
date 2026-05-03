using UnityEngine;

public class MainCamera : MonoBehaviour
{

    public int maxRecursionDepth = 3;
    Portal[] portals;

    void Awake()
    {
        portals = FindObjectsByType<Portal>();
    }

    void LateUpdate()
    {

        // for (int i = 0; i < portals.Length; i++) {
        //     portals[i].PrePortalRender ();
        // }
        for (int i = 0; i < portals.Length; i++)
        {
            portals[i].Render();
        }

        // for (int i = 0; i < portals.Length; i++) {
        //     portals[i].PostPortalRender ();
        // }
    }

}