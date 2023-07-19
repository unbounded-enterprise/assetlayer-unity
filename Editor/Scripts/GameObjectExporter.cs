using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;

public class ExportToObjAndZip
{
    [MenuItem("GameObject/Export to OBJ and Zip", priority = 0)]
    public static void ExportToOBJ()
    {
        GameObject obj = Selection.activeGameObject;
        if (obj == null)
        {
            Debug.LogError("No GameObject selected.");
            return;
        }

        MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            Debug.LogError("The selected GameObject does not have a MeshFilter component.");
            return;
        }

        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogError("The selected GameObject does not have a Renderer component.");
            return;
        }

        string folderPath = Application.dataPath + "/" + obj.name;
        Directory.CreateDirectory(folderPath);

        // Export the .obj file.
        string objPath = folderPath + "/" + obj.name + ".obj";
        ExportMeshToObj(meshFilter.sharedMesh, objPath);

        // Export the .mtl file and textures.
        string mtlPath = folderPath + "/" + obj.name + ".mtl";
        ExportMaterialToMtl(renderer.sharedMaterial, mtlPath, folderPath);

        // Update the .obj file to reference the .mtl file.
        File.AppendAllText(objPath, $"mtllib {obj.name}.mtl");

        // Zip the folder.
        string zipPath = Application.dataPath + "/Assets" + obj.name + ".zip";
        ZipFile.CreateFromDirectory(folderPath, zipPath);

        Debug.Log("Exported to: " + zipPath);
    }

    private static void ExportMeshToObj(Mesh mesh, string path)
    {
        using (StreamWriter sw = new StreamWriter(path))
        {
            sw.WriteLine("o " + mesh.name);

            foreach (Vector3 v in mesh.vertices)
            {
                sw.WriteLine($"v {v.x} {v.y} {v.z}");
            }

            foreach (Vector3 v in mesh.normals)
            {
                sw.WriteLine($"vn {v.x} {v.y} {v.z}");
            }

            foreach (Vector2 v in mesh.uv)
            {
                sw.WriteLine($"vt {v.x} {v.y}");
            }

            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                int[] triangles = mesh.GetTriangles(i);
                for (int j = 0; j < triangles.Length; j += 3)
                {
                    sw.WriteLine($"f {triangles[j + 2] + 1}/{triangles[j + 2] + 1}/{triangles[j + 2] + 1} {triangles[j + 1] + 1}/{triangles[j + 1] + 1}/{triangles[j + 1] + 1} {triangles[j] + 1}/{triangles[j] + 1}/{triangles[j] + 1}");
                }
            }
        }
    }

    private static void ExportMaterialToMtl(Material mat, string path, string folderPath)
    {
        using (StreamWriter sw = new StreamWriter(path))
        {
            sw.WriteLine("newmtl " + mat.name);

            Color col = mat.color;
            sw.WriteLine($"Kd {col.r} {col.g} {col.b}");

            if (mat.HasProperty("_MainTex"))
            {
                Texture2D tex = mat.mainTexture as Texture2D;
                if (tex != null)
                {
                    // Create a temporary readable copy of the texture
                    RenderTexture rt = RenderTexture.GetTemporary(tex.width, tex.height);
                    Graphics.Blit(tex, rt);
                    RenderTexture previous = RenderTexture.active;
                    RenderTexture.active = rt;

                    Texture2D readableText = new Texture2D(tex.width, tex.height);
                    readableText.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                    readableText.Apply();

                    RenderTexture.active = previous;
                    RenderTexture.ReleaseTemporary(rt);

                    byte[] texBytes = readableText.EncodeToPNG();
                    string texPath = $"{folderPath}/{tex.name}.png";
                    File.WriteAllBytes(texPath, texBytes);
                    sw.WriteLine($"map_Kd {tex.name}.png");

                    // Dispose of the temporary texture
                    GameObject.DestroyImmediate(readableText);
                }
            }
        }
    }
}

