using UnityEngine;

public class AutoWalls : MonoBehaviour
{
    public float wallHeight = 4.0f;
    public float wallThickness = 0.2f;
    public Material wallMaterial;
    [Header("Layer Settings")]
    public LayerMask wallLayer;

    void Start()
    {
        CreateWalls();
    }

    void CreateWalls()
    {
        // Plane 기본 크기 = 10
        float planeSize = 10f;

        // 실제 크기 계산
        Vector3 scale = transform.localScale;
        float width = planeSize * scale.x;
        float length = planeSize * scale.z;

        Vector3 center = transform.position;

        // 벽 4개 생성
        CreateWall("North", center + new Vector3(0, wallHeight / 2, length / 2), planeSize, wallHeight);
        CreateWall("South", center + new Vector3(0, wallHeight / 2, -length / 2), planeSize, wallHeight);
        CreateWall("East", center + new Vector3(width / 2, wallHeight / 2, 0), planeSize, wallHeight, true);
        CreateWall("West", center + new Vector3(-width / 2, wallHeight / 2, 0), planeSize, wallHeight, true);
    }

    void CreateWall(string name, Vector3 position, float width, float height, bool rotate = false)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name + "_Wall";
        wall.transform.parent = transform;
        wall.transform.position = position;

        wall.layer = GetFirstLayerFromMask(wallLayer);

        if (rotate)
        {
            wall.transform.localScale = new Vector3(wallThickness, height, width);
        }
        else
        {
            wall.transform.localScale = new Vector3(width, height, wallThickness);
        }

        if (wallMaterial != null)
        {
            wall.GetComponent<Renderer>().material = wallMaterial;
        }
    }

    int GetFirstLayerFromMask(LayerMask mask)
    {
        int layer = 0;
        int maskValue = mask.value;
        while (maskValue > 1)
        {
            maskValue >>= 1;
            layer++;
        }
        return layer;
    }
}