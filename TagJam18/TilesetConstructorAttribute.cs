using System;

namespace TagJam18
{
    [AttributeUsage(AttributeTargets.Constructor)]
    class TilesetConstructorAttribute : Attribute
    {
        public TilesetConstructorAttribute(int id)
        {
            if (id < 1) // id of 0 is empty space
            { throw new ArgumentOutOfRangeException("id"); }

            this.Id = id;
        }

        public int Id { get; private set; }
    }
}
