using System;
using SharpDX;

namespace TagJam18
{
    internal static class SharpDXMathEx
    {
        public static Vector3 Normalized(this Vector3 v)
        {
            Vector3 ret = v;
            ret.Normalize();
            return ret;
        }
    }

    internal static class Vector3Ex
    {
        public static Vector3 Project(Vector3 vector, ViewportF viewport, Matrix worldViewProjection)
        {
            return Vector3.Project(vector, viewport.X, viewport.Y, viewport.Width, viewport.Height, viewport.MinDepth, viewport.MaxDepth, worldViewProjection);
        }
    }
}
