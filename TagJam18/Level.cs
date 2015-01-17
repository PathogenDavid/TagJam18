using System;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using System.IO;
using SharpDX.Toolkit;

namespace TagJam18
{
    public class Level : Entity
    {
        private delegate Entity TilesetConstructorDelegate(TagGame parentGame, float x, float y);
        private static Dictionary<int, ConstructorInfo> entityConstructors;

        public float Width { get; private set; }
        public float Height { get; private set; }

        static Level()
        {
            entityConstructors = new Dictionary<int, ConstructorInfo>();

            foreach (Type type in typeof(Level).Assembly.GetTypes())
            foreach (ConstructorInfo constructor in type.GetConstructors())
            foreach (TilesetConstructorAttribute attrib in constructor.GetCustomAttributes<TilesetConstructorAttribute>(false))
            {
                if (!type.IsSubclassOf(typeof(Entity)))
                { throw new InvalidOperationException("TilesetConstructorAttribute can only be applied to classes which implement the Entity class."); }

                if (entityConstructors.ContainsKey(attrib.Id))
                { throw new InvalidOperationException("This assembly has more than one Entity that can construct " + attrib.Id.ToString() + "."); }

                //TODO: Ensure parameters match.
                //TODO: Ensure function is implemented.

                entityConstructors.Add(attrib.Id, constructor);
            }
        }

        private TagGame parentGame;

        public Level(TagGame parentGame, string file) : base(parentGame)
        {
            this.parentGame = parentGame;

            using (StreamReader f = new StreamReader(file))
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(f);
                XmlNodeSimple xml = xmlDocument;

                int w = Convert.ToInt32(xml["//map/@width"].Value);
                int h = Convert.ToInt32(xml["//map/@height"].Value);
                Width = (float)w;
                Height = (float)h;

                // Parse tile layers
                XmlNodeList layers = xml.SelectNodes("//map/layer");
                if (layers.Count != 1)
                { throw new ArgumentException("The given level file is invalid, it has the incorrect number of tile layers.", "file"); }

                XmlNodeSimple layer = layers[0];
                if (Convert.ToInt32(layer["@width"].Value) != w || Convert.ToInt32(layer["@height"].Value) != h)
                { throw new ArgumentException("The given level file is invalid, it contains tile layers that don't match the map dimensions.", "file"); }

                if (layer["data/@encoding"].Value != "csv")
                { throw new ArgumentException("The given level file is invalid, it uses a tile layer encoding other than CSV.", "file"); }

                ProcessCsv(layer["data/text()"].Value);

                //TODO: Parse object layers
            }
        }

        private void ProcessCsv(string csv)
        {
            int x = -1;
            int y = -1;
            string[] csvLines = csv.Replace("\r", "").Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in csvLines)
            {
                y++;
                x = -1;

                string[] csvTokens = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string token in csvTokens)
                {
                    x++;

                    int id = Convert.ToInt32(token);
                    if (id == 0) { continue; }

                    MakeEntity(id, (float)x, (float)y);
                }
            }
        }

        private void MakeEntity(int id, float x, float y)
        {
            if (!entityConstructors.ContainsKey(id))
            {
                Debug.Print("WARNING: I don't know how to make an entity of ID {0} at {1},{2}!", id, x, y);
                return;
            }

            entityConstructors[id].Invoke(parentGame, x, y);
        }

        public override void Render(GameTime gameTime)
        {
            
        }
    }
}
