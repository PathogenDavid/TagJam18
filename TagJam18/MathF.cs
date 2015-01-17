using System;

namespace TagJam18
{
    class MathF
    {
        public static float Pi
        {
            get { return (float)Math.PI; }
        }

        public static float Sin(float x)
        {
            return Sin((double)x);
        }

        public static float Sin(double x)
        {
            return (float)Math.Sin(x);
        }
    }
}
