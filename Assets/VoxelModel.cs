using UnityEngine;
using System.Collections.Generic;

// Builds a blocky 3D mesh from a "pixel art" pattern, like the sprites of the original arcade game.
// Each row is a string; every character equal to `cell` becomes one cube. Faces hidden between
// neighboring cubes are skipped, and meshes are cached so each pattern is only built once.
public static class VoxelModel
{
    static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();
    static readonly Vector3[] directions =
    {
        Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back
    };

    public static Mesh Build(string[] rows, char cell, float pixelSize, float depth)
    {
        string key = string.Join("/", rows) + "|" + cell + "|" + pixelSize + "|" + depth;
        // Meshes made at runtime can be unloaded on a scene change, so check the cached one still exists.
        if (cache.TryGetValue(key, out Mesh cached) && cached != null)
        {
            return cached;
        }

        int height = rows.Length, width = 0;
        foreach (string row in rows)
        {
            width = Mathf.Max(width, row.Length);
        }

        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<int> triangles = new List<int>();
        Vector3 half = new Vector3(pixelSize, pixelSize, depth) / 2;

        for (int r = 0; r < height; r++)
        {
            for (int c = 0; c < rows[r].Length; c++)
            {
                if (rows[r][c] != cell)
                {
                    continue;
                }
                Vector3 center = new Vector3((c - (width - 1) / 2f) * pixelSize, ((height - 1) / 2f - r) * pixelSize, 0);

                foreach (Vector3 n in directions)
                {
                    // Rows go downward, so a neighbor "up" is in the previous row.
                    if (n.z == 0 && IsFilled(rows, cell, r - (int)n.y, c + (int)n.x))
                    {
                        continue;
                    }
                    Vector3 u = n.x != 0 ? Vector3.up : Vector3.right;
                    Vector3 v = n.z != 0 ? Vector3.up : Vector3.forward;
                    Vector3 faceCenter = center + Vector3.Scale(n, half);
                    Vector3 du = Vector3.Scale(u, half), dv = Vector3.Scale(v, half);

                    int start = vertices.Count;
                    vertices.Add(faceCenter - du - dv);
                    vertices.Add(faceCenter + du - dv);
                    vertices.Add(faceCenter + du + dv);
                    vertices.Add(faceCenter - du + dv);
                    for (int i = 0; i < 4; i++)
                    {
                        normals.Add(n);
                    }

                    // Unity draws a triangle's front side when its corners wind clockwise as seen from the front.
                    bool facesOut = Vector3.Dot(Vector3.Cross(vertices[start + 1] - vertices[start], vertices[start + 2] - vertices[start]), n) > 0;
                    if (facesOut)
                    {
                        triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
                    }
                    else
                    {
                        triangles.AddRange(new[] { start, start + 2, start + 1, start, start + 3, start + 2 });
                    }
                }
            }
        }

        // DontSave: these meshes are generated, never written into the scene (also when previewed in the editor).
        Mesh mesh = new Mesh { name = "Voxel", hideFlags = HideFlags.DontSave };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        cache[key] = mesh;
        return mesh;
    }

    static bool IsFilled(string[] rows, char cell, int r, int c)
    {
        return r >= 0 && r < rows.Length && c >= 0 && c < rows[r].Length && rows[r][c] == cell;
    }
}
