using System;

namespace Evolve.Combat
{
    /// <summary>Plain mesh data: x, y, z of each vertex one after another, and triangle indices (no Unity types, so it can be tested).</summary>
    public sealed class MeshData
    {
        public float[] Vertices;
        public int[] Triangles;
        public int VertexCount => Vertices.Length / 3;
    }

    /// <summary>The shapes of a boss's clothes and ornaments (crown, scarf ring, cape) built in code, and the wind that blows the cape.</summary>
    public static class RegaliaMeshes
    {
        /// <summary>A ring round the Y axis: <paramref name="major"/> is the distance of the tube's centre from the axis, <paramref name="minor"/> the tube's radius.</summary>
        public static MeshData Torus(float major, float minor, int majorSegments, int minorSegments)
        {
            var v = new float[majorSegments * minorSegments * 3];
            var t = new int[majorSegments * minorSegments * 6];
            for (int i = 0; i < majorSegments; i++)
                for (int j = 0; j < minorSegments; j++)
                {
                    float a = (float)(2.0 * Math.PI * i / majorSegments), b = (float)(2.0 * Math.PI * j / minorSegments);
                    float r = major + minor * (float)Math.Cos(b);
                    int k = (i * minorSegments + j) * 3;
                    v[k] = r * (float)Math.Cos(a); v[k + 1] = minor * (float)Math.Sin(b); v[k + 2] = r * (float)Math.Sin(a);
                    int i2 = (i + 1) % majorSegments, j2 = (j + 1) % minorSegments;
                    int p00 = i * minorSegments + j, p10 = i2 * minorSegments + j, p11 = i2 * minorSegments + j2, p01 = i * minorSegments + j2;
                    int q = (i * minorSegments + j) * 6;
                    t[q] = p00; t[q + 1] = p10; t[q + 2] = p11; t[q + 3] = p00; t[q + 4] = p11; t[q + 5] = p01;
                }
            return new MeshData { Vertices = v, Triangles = t };
        }

        /// <summary>Four-sided pointed spikes standing on a circle of <paramref name="radius"/> in the x-z plane, each <paramref name="height"/> high.</summary>
        public static MeshData Crown(float radius, float height, int spikes)
        {
            var v = new float[spikes * 5 * 3];
            var t = new int[spikes * 6 * 3];
            float w = radius * 0.18f;
            for (int s = 0; s < spikes; s++)
            {
                float a = (float)(2.0 * Math.PI * s / spikes);
                float cx = (float)Math.Cos(a), cz = (float)Math.Sin(a), tx = -cz, tz = cx;   // outward and sideways unit vectors
                int b = s * 5;
                float[,] corner = { { -1, -1 }, { 1, -1 }, { 1, 1 }, { -1, 1 } };
                for (int c = 0; c < 4; c++)
                {
                    int k = (b + c) * 3;
                    v[k] = cx * radius + cx * corner[c, 0] * w + tx * corner[c, 1] * w; v[k + 1] = 0f; v[k + 2] = cz * radius + cz * corner[c, 0] * w + tz * corner[c, 1] * w;
                }
                int ap = (b + 4) * 3;
                v[ap] = cx * radius; v[ap + 1] = height; v[ap + 2] = cz * radius;
                int[] tri = { 0, 1, 4, 1, 2, 4, 2, 3, 4, 3, 0, 4, 0, 2, 1, 0, 3, 2 };
                for (int q = 0; q < 18; q++) t[s * 18 + q] = b + tri[q];
            }
            return new MeshData { Vertices = v, Triangles = t };
        }

        /// <summary>A flat sheet in the x-z plane: x across (<paramref name="width"/> wide, centred), z from 0 at the shoulder line down to <c>-length</c> at the hem, y = 0.</summary>
        public static MeshData CapeGrid(float width, float length, int cols, int rows)
        {
            var v = new float[(cols + 1) * (rows + 1) * 3];
            var t = new int[cols * rows * 6];
            for (int r = 0; r <= rows; r++)
                for (int c = 0; c <= cols; c++)
                {
                    int k = (r * (cols + 1) + c) * 3;
                    v[k] = -width / 2f + width * c / cols; v[k + 1] = 0f; v[k + 2] = -length * r / rows;
                }
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    int i = r * (cols + 1) + c, q = (r * cols + c) * 6;
                    t[q] = i; t[q + 1] = i + cols + 1; t[q + 2] = i + 1; t[q + 3] = i + 1; t[q + 4] = i + cols + 1; t[q + 5] = i + cols + 2;
                }
            return new MeshData { Vertices = v, Triangles = t };
        }

        /// <summary>Sideways flutter of the cape in the wind, as a fraction of its sway range: none at the shoulders (v = 0), most at the hem (v = 1), always within [-1, 1].</summary>
        public static float CapeWave(float u, float v, float time)
        {
            float wave = 0.6f * (float)Math.Sin(time * 2.2 + v * 5.0 + u * 2.0) + 0.4f * (float)Math.Sin(time * 3.7 - u * 3.0 + v * 3.0);
            return v * v * wave;
        }
    }
}
