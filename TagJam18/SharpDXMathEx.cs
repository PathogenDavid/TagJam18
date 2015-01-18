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
}
