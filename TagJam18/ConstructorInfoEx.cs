using System;
using System.Reflection;

namespace TagJam18
{
    internal static class ConstructorInfoEx
    {
        internal static Object Invoke(this ConstructorInfo constructor, params object[] parameters)
        {
            return constructor.Invoke(parameters);
        }
    }
}
