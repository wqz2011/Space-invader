using UnityEngine;
using System.Collections.Generic;

// Turns a small "pixel art" picture into a 3D model made of cubes.
// The picture is an array of strings, one string per row. Every character that matches
// "cell" becomes a cube. For example "X" might be the hull and "." empty space.
public static class VoxelModel
{
    public static Mesh Build(string[] rows, char cell, float pixelSize, float depth)
    {
        Mesh cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        List<CombineInstance> cubes = new List<CombineInstance>();

        int height = rows.Length;
        int width = rows[0].Length;

        for (int row = 0; row < height; row++)
        {
            for (int col = 0; col < rows[row].Length; col++)
            {
                if (rows[row][col] != cell)
                {
                    continue;
                }

                // Center the picture on (0, 0). Row 0 is the top, so y goes down as row goes up.
                float x = (col - (width - 1) / 2f) * pixelSize;
                float y = ((height - 1) / 2f - row) * pixelSize;

                CombineInstance piece = new CombineInstance();
                piece.mesh = cube;
                piece.transform = Matrix4x4.TRS(new Vector3(x, y, 0), Quaternion.identity, new Vector3(pixelSize, pixelSize, depth));
                cubes.Add(piece);
            }
        }

        // Merge all the cubes into one mesh, so each model is drawn in one go instead of one cube at a time.
        Mesh mesh = new Mesh();
        mesh.name = "Voxel";
        mesh.CombineMeshes(cubes.ToArray());
        return mesh;
    }
}
