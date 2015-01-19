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

        public static float Cos(float x)
        {
            return Cos((double)x);
        }

        public static float Cos(double x)
        {
            return (float)Math.Cos(x);
        }

        public static float Pow(float x, float y)
        {
            return (float)Math.Pow(x, y);
        }
    }
}
