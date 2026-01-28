using UnityEngine;
using UnityEditor;

public class TerrainGrassExtractor
{
    [MenuItem("Tools/Terrain/Extract Grass To Prefabs")]
    static void ExtractGrass()
    {
        Terrain terrain = Selection.activeGameObject?.GetComponent<Terrain>();
        if (terrain == null)
        {
            Debug.LogError("❌ Terrain tanlanmagan!");
            return;
        }

        TerrainData data = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;

        GameObject grassRoot = new GameObject("Grass_Root");

        int detailWidth = data.detailWidth;
        int detailHeight = data.detailHeight;

        for (int layer = 0; layer < data.detailPrototypes.Length; layer++)
        {
            DetailPrototype proto = data.detailPrototypes[layer];
            if (proto.prototype == null) continue;

            int[,] detailMap = data.GetDetailLayer(0, 0, detailWidth, detailHeight, layer);

            for (int x = 0; x < detailWidth; x++)
            {
                for (int y = 0; y < detailHeight; y++)
                {
                    int count = detailMap[x, y];
                    if (count == 0) continue;

                    for (int i = 0; i < count; i++)
                    {
                        float nx = (float)x / detailWidth;
                        float ny = (float)y / detailHeight;

                        float worldX = nx * data.size.x;
                        float worldZ = ny * data.size.z;
                        float worldY = data.GetInterpolatedHeight(nx, ny);

                        Vector3 pos = terrainPos + new Vector3(worldX, worldY, worldZ);

                        GameObject grass = (GameObject)PrefabUtility.InstantiatePrefab(proto.prototype);
                        grass.transform.position = pos;
                        grass.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
                        grass.transform.localScale *= Random.Range(0.8f, 1.2f);
                        grass.transform.SetParent(grassRoot.transform);
                    }
                }
            }
        }

        Debug.Log("✅ Grass extraction completed!");
    }
}
