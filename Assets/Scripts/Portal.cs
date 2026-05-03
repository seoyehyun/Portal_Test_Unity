using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour
{
    public Portal linkedPortal;
    public MeshRenderer screen;
    public float nearClipOffset = 0.01f;
    public float nearClipLimit = 0.2f;
    public float detectionRadius = 2.0f;
    public LayerMask obstacleLayer;
    Camera playerCam;
    Camera portalCam;
    RenderTexture viewTexture;
    List<PortalTraveller> trackedTravellers = new List<PortalTraveller>();
    private List<Collider> ignoredColliders = new List<Collider>();

    void Awake()
    {
        playerCam = Camera.main;
        portalCam = GetComponentInChildren<Camera>();
        portalCam.enabled = false;
    }

    void LateUpdate()
    {
        for (int i = 0; i < trackedTravellers.Count; i++)
        {
            PortalTraveller traveller = trackedTravellers[i];
            Transform travellerT = traveller.transform;

            Vector3 OffsetFromPortal = travellerT.position - transform.position;

            int portalSide = System.Math.Sign(Vector3.Dot(OffsetFromPortal, transform.forward));
            int portalSideOld = System.Math.Sign(Vector3.Dot(traveller.previousOffsetFromPortal, transform.forward));

            Debug.Log("portalSide: " + portalSide + ", portalSideOld: " + portalSideOld);
            if (portalSide != portalSideOld)
            {
                var m = linkedPortal.transform.localToWorldMatrix * transform.worldToLocalMatrix * travellerT.localToWorldMatrix;
                traveller.Teleport(transform, linkedPortal.transform, m.GetColumn(3), m.rotation);

                // incase I can't rely on OnTriggerEnter/Exit to be called next frame
                //linkedPortal.OnTravellerEnterPortal(traveller);
                // trackedTravellers.RemoveAt(i);
                // i--;
            }
            else
            {
                traveller.previousOffsetFromPortal = OffsetFromPortal;

            }
        }
    }

    void CreateViewTexture()
    {
        if (viewTexture == null || viewTexture.width != Screen.width || viewTexture.height != Screen.height)
        {
            if (viewTexture != null)
            {
                viewTexture.Release();
            }
            viewTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.DefaultHDR);
            portalCam.targetTexture = viewTexture;

            linkedPortal.screen.GetComponent<Renderer>().material.SetTexture("_MainTex", viewTexture);
            linkedPortal.screen.GetComponent<Renderer>().material.SetInt("displayMask", 1);
        }
    }

    static bool VisibleFromCamera(Renderer renderer, Camera camera)
    {
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);
        return GeometryUtility.TestPlanesAABB(frustumPlanes, renderer.bounds);
    }

    public void Render()
    {
        if (!VisibleFromCamera(linkedPortal.screen, playerCam))
        {
            return;
        }
        
        //playerCam과 세팅 맞추기
        portalCam.projectionMatrix = playerCam.projectionMatrix;
        portalCam.fieldOfView = playerCam.fieldOfView;
        portalCam.aspect = playerCam.aspect;
        portalCam.renderingPath = playerCam.renderingPath;

        screen.enabled = false;
        CreateViewTexture();

        var m = transform.localToWorldMatrix * linkedPortal.transform.worldToLocalMatrix * playerCam.transform.localToWorldMatrix;
        portalCam.transform.SetPositionAndRotation(m.GetColumn(3), m.rotation);
        SetNearClipPlane();
        portalCam.Render();
        screen.enabled = true;
    }

    void SetNearClipPlane()
    {
        // Learning resource:
        // http://www.terathon.com/lengyel/Lengyel-Oblique.pdf
        Transform clipPlane = transform;
        int dot = System.Math.Sign(Vector3.Dot(clipPlane.forward, transform.position - portalCam.transform.position));

        Vector3 camSpacePos = portalCam.worldToCameraMatrix.MultiplyPoint(clipPlane.position);
        Vector3 camSpaceNormal = portalCam.worldToCameraMatrix.MultiplyVector(clipPlane.forward) * dot;
        float camSpaceDst = -Vector3.Dot(camSpacePos, camSpaceNormal) + nearClipOffset;

        // Don't use oblique clip plane if very close to portal as it seems this can cause some visual artifacts
        if (Mathf.Abs(camSpaceDst) > nearClipLimit)
        {
            Vector4 clipPlaneCameraSpace = new Vector4(camSpaceNormal.x, camSpaceNormal.y, camSpaceNormal.z, camSpaceDst);

            // Update projection based on new clip plane
            // Calculate matrix with player cam so that player camera settings (fov, etc) are used
            portalCam.projectionMatrix = playerCam.CalculateObliqueMatrix(clipPlaneCameraSpace);
        }
        else
        {
            portalCam.projectionMatrix = playerCam.projectionMatrix;
        }
    }

    void OnTravellerEnterPortal(PortalTraveller traveller)
    {
        if (!trackedTravellers.Contains(traveller))
        {
            //traveller.EnterPortalThreshold();
            traveller.previousOffsetFromPortal = traveller.transform.position - transform.position;
            trackedTravellers.Add(traveller);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"충돌 감지! 내 이름: {gameObject.name}, 부딪힌 대상: {other.name}");
        var traveller = other.GetComponent<PortalTraveller>();
        if (traveller != null)
        {
            // 현재 포털에서 other의 반대쪽 collider 무시
            Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius, obstacleLayer);
            foreach (Collider col in colliders)
            {
                Vector3 dirToCol = col.transform.position - transform.position;
                Vector3 dirToOther = other.transform.position - transform.position;
                if (Vector3.Dot(dirToOther, dirToCol) < 0) // other의 반대쪽 확인
                {
                    Physics.IgnoreCollision(other, col, true);
                    ignoredColliders.Add(col);
                }
                // Physics.IgnoreCollision(other, col, true);
                // ignoredColliders.Add(col);
            }

            // linkedPortal에서도 other의 반대쪽 collider 무시
            Collider[] linkedColliders = Physics.OverlapSphere(linkedPortal.transform.position, linkedPortal.detectionRadius, linkedPortal.obstacleLayer);
            foreach (Collider col in linkedColliders)
            {
                Vector3 dirToCol = col.transform.position - linkedPortal.transform.position;
                Vector3 dirToOther = other.transform.position - linkedPortal.transform.position;
                if (Vector3.Dot(dirToOther, dirToCol) < 0) // other의 반대쪽 확인
                {
                    Physics.IgnoreCollision(other, col, true);
                    ignoredColliders.Add(col);
                }
                // Physics.IgnoreCollision(other, col, true);
                // ignoredColliders.Add(col);
            }

            OnTravellerEnterPortal(traveller);
        }
    }

    void OnTriggerExit(Collider other)
    {
        var traveller = other.GetComponent<PortalTraveller>();
        if (traveller && trackedTravellers.Contains(traveller))
        {
            foreach (Collider col in ignoredColliders)
            {
                if (col != null) Physics.IgnoreCollision(other, col, false);
            }
            ignoredColliders.Clear();

            traveller.ExitPortalThreshold();
            trackedTravellers.Remove(traveller);
        }
    }

    // Debug, Vizualization
    // void OnDrawGizmos()
    // {
    //     // IgnoreCollision이 적용된 collider들을 빨간색으로 표시
    //     Gizmos.color = Color.yellow;
    //     foreach (Collider col in ignoredColliders)
    //     {
    //         if (col != null)
    //         {
    //             Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    //         }
    //     }

    //     // 탐지 반경을 파란색 구로 표시
    //     Gizmos.color = Color.blue;
    //     Gizmos.DrawWireSphere(transform.position, detectionRadius);
    // }
}
