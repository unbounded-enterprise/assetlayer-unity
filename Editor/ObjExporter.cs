using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;

public class ObjExporter : MonoBehaviour
{
    [MenuItem("GameObject/Export to OBJ")]
    static void ExportToOBJ()
    {
        GameObject obj = Selection.activeGameObject;
        if (obj == null)
        {
            Debug.LogError("No GameObject selected.");
            return;
        }

        MeshFilter mf = obj.GetComponent<MeshFilter>();
        if (mf == null)
        {
            Debug.LogError("Selected GameObject does not have a MeshFilter component.");
            return;
        }

        Mesh mesh = mf.sharedMesh;

        StringBuilder sb = new StringBuilder();

        sb.Append("g ").Append(obj.name).Append("\n");

        foreach (Vector3 v in mesh.vertices)
        {
            sb.Append(string.Format("v {0} {1} {2}\n", v.x, v.y, v.z));
        }

        foreach (Vector3 v in mesh.normals)
        {
            sb.Append(string.Format("vn {0} {1} {2}\n", v.x, v.y, v.z));
        }

        foreach (Vector3 v in mesh.uv)
        {
            sb.Append(string.Format("vt {0} {1}\n", v.x, v.y));
        }

        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            sb.Append("\n");
            int[] triangles = mesh.GetTriangles(i);
            for (int j = 0; j < triangles.Length; j += 3)
            {
                sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n",
                    triangles[j] + 1, triangles[j + 1] + 1, triangles[j + 2] + 1));
            }
        }

        using (StreamWriter sw = new StreamWriter("" + obj.name + ".obj"))
        {
            sw.Write(sb.ToString());
        }
    }
}
