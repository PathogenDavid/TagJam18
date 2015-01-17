using System;

namespace TagJam18
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            using (TagGame game = new TagGame())
            {
                game.Run();
            }
        }
    }
}
