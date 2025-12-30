#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class VertexColorBaker : EditorWindow
{
    private enum Axis { X, Y, Z }

    private Axis heightAxis = Axis.Z;
    private float power = 1.5f;
    private bool invert = false;

    [MenuItem("Tools/Kopkari/Vertex Color Baker")]
    public static void ShowWindow() => GetWindow<VertexColorBaker>("Vertex Color Baker");

    private void OnGUI()
    {
        GUILayout.Label("Bake Bottom Gradient into VertexColor.R", EditorStyles.boldLabel);

        heightAxis = (Axis)EditorGUILayout.EnumPopup("Axis (Bottom°ÊTop)", heightAxis);
        power = EditorGUILayout.Slider("Power", power, 0.1f, 6f);
        invert = EditorGUILayout.Toggle("Invert", invert);

        EditorGUILayout.Space(8);

        if (GUILayout.Button("Bake to NEW Mesh Asset (Selected Renderer)"))
            BakeSelected();
    }

    private void BakeSelected()
    {
        var go = Selection.activeGameObject;
        if (!go) { Debug.LogError("No GameObject selected."); return; }

        Mesh sourceMesh = null;
        bool isSkinned = false;

        var smr = go.GetComponent<SkinnedMeshRenderer>();
        var mf = go.GetComponent<MeshFilter>();

        if (smr && smr.sharedMesh) { sourceMesh = smr.sharedMesh; isSkinned = true; }
        else if (mf && mf.sharedMesh) sourceMesh = mf.sharedMesh;

        if (!sourceMesh) { Debug.LogError("No mesh found on selected object."); return; }

        var newMesh = Instantiate(sourceMesh);
        newMesh.name = sourceMesh.name + "_VCBaked_" + heightAxis;

        var verts = newMesh.vertices;
        if (verts == null || verts.Length == 0) { Debug.LogError("Mesh has no vertices."); return; }

        // Get min/max along selected axis (local space)
        float min = float.PositiveInfinity, max = float.NegativeInfinity;
        for (int i = 0; i < verts.Length; i++)
        {
            float v = GetAxisValue(verts[i], heightAxis);
            if (v < min) min = v;
            if (v > max) max = v;
        }
        float range = Mathf.Max(0.0001f, max - min);

        var colors = new Color[verts.Length];
        for (int i = 0; i < verts.Length; i++)
        {
            float v = GetAxisValue(verts[i], heightAxis);
            float t = (v - min) / range; // bottom=0 top=1 along chosen axis

            // Want bottom=1 top=0
            float r = 1f - Mathf.Pow(Mathf.Clamp01(t), power);
            if (invert) r = 1f - r;

            colors[i] = new Color(r, 0f, 0f, 1f);
        }

        newMesh.colors = colors;

        const string folder = "Assets/GeneratedMeshes";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "GeneratedMeshes");

        string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{newMesh.name}.asset");
        AssetDatabase.CreateAsset(newMesh, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (isSkinned) { smr.sharedMesh = newMesh; EditorUtility.SetDirty(smr); }
        else { mf.sharedMesh = newMesh; EditorUtility.SetDirty(mf); }

        Debug.Log($"Baked VertexColor.R along {heightAxis} and saved: {path}");
    }

    private static float GetAxisValue(Vector3 v, Axis axis)
    {
        return axis switch
        {
            Axis.X => v.x,
            Axis.Y => v.y,
            Axis.Z => v.z,
            _ => v.y
        };
    }
}
#endif
