using UnityEngine;
using UnityEngine.Rendering;

namespace Evolve.World
{
    /// <summary>Half-ellipsoid dome (a cave roof) whose faces and normals point inward, toward the player below.</summary>
    public static class DomeMesh
    {
        /// <param name="radius">Horizontal radius at the rim (y = 0).</param>
        /// <param name="height">Height of the apex above the rim.</param>
        public static Mesh Build(float radius, float height, int segments, int rings)
        {
            int cols = segments + 1;
            int vertexCount = (rings + 1) * cols;
            var vertices = new Vector3[vertexCount];
            var normals = new Vector3[vertexCount];
            var uvs = new Vector2[vertexCount];

            for (int i = 0; i <= rings; i++)
            {
                float phi = (float)i / rings * Mathf.PI * 0.5f; // 0 at the apex, 90 degrees at the rim
                float ring = radius * Mathf.Sin(phi);
                float y = height * Mathf.Cos(phi);
                for (int j = 0; j <= segments; j++)
                {
                    float theta = (float)j / segments * Mathf.PI * 2f;
                    float x = ring * Mathf.Cos(theta), z = ring * Mathf.Sin(theta);
                    int k = i * cols + j;
                    vertices[k] = new Vector3(x, y, z);
                    // outward ellipsoid normal ~ (x/R^2, y/H^2, z/R^2); the dome is seen from inside, so flip it
                    Vector3 outward = new Vector3(x / (radius * radius), y / (height * height), z / (radius * radius));
                    normals[k] = outward.sqrMagnitude > 1e-12f ? -outward.normalized : Vector3.down;
                    uvs[k] = new Vector2((float)j / segments, (float)i / rings);
                }
            }

            var triangles = new int[rings * segments * 6];
            int t = 0;
            for (int i = 0; i < rings; i++)
            {
                for (int j = 0; j < segments; j++)
                {
                    int v00 = i * cols + j, v01 = v00 + 1, v10 = (i + 1) * cols + j, v11 = v10 + 1;
                    triangles[t++] = v00; triangles[t++] = v10; triangles[t++] = v01;
                    triangles[t++] = v01; triangles[t++] = v10; triangles[t++] = v11;
                }
            }

            var mesh = new Mesh { name = "Underroot Dome" };
            if (vertexCount > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
