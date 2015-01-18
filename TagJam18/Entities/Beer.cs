using System;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;

namespace TagJam18.Entities
{
    class Beer : Entity
    {
        public Vector3 Position { get; private set; }
        private Model mesh;
        private const string meshId = "Beer/Mesh";
        private Texture2D texture;
        private const string textureId = "Beer/Texture";
        private Matrix baseTransform;

        [TilesetConstructor(2)]
        public Beer(TagGame parentGame, int x, int y)
            : base(parentGame)
        {
            this.Position = new Vector3((float)x, (float)y, 0f);
            mesh = ParentGame.Resources.Get<Model>(meshId, () => parentGame.Content.Load<Model>("BeerBottle"));
            texture = ParentGame.Resources.Get<Texture2D>(textureId, () => parentGame.Content.Load<Texture2D>("BeerBottleTexture"));

            Random r = new Random();

            const float idealRotation = -1f; // Has the label facing the camera for our default camera angle.
            float rotationVariance = MathF.Pi / 2f;
            float rotation = r.NextFloat(-rotationVariance, rotationVariance) + idealRotation;

            const float positionVariance = 0.3f;
            float xOff = r.NextFloat(-positionVariance, positionVariance);
            float yOff = r.NextFloat(-positionVariance, positionVariance);

            const float scalingVariance = 0.05f;
            float scaling = 0.55f + r.NextFloat(-scalingVariance, scalingVariance);

            // Flip the model over * orient bottle * move bottom of bottle to proper bottom * make bottle smaller * apply random position offset
            baseTransform = Matrix.RotationX(MathF.Pi) * Matrix.RotationZ(rotation) * Matrix.Translation(0f, 0f, -1f) * Matrix.Scaling(scaling) * Matrix.Translation(xOff, yOff, 0f);
        }

        public override void Render(GameTime gameTime)
        {
            ParentGame.BasicEffect.World = baseTransform * Matrix.Translation(Position);
            ParentGame.BasicEffect.Texture = texture;
            ParentGame.BasicEffect.TextureEnabled = true;
            float oldSpecularPower = ParentGame.BasicEffect.SpecularPower;
            ParentGame.BasicEffect.SpecularPower = 64f;

            mesh.Draw(ParentGame.GraphicsDevice, ParentGame.BasicEffect.World, ParentGame.BasicEffect.View, ParentGame.BasicEffect.Projection, ParentGame.BasicEffect);

            ParentGame.BasicEffect.SpecularPower = oldSpecularPower;
            ParentGame.BasicEffect.TextureEnabled = false;
            ParentGame.BasicEffect.Texture = null;
        }

        protected override void Dispose(bool disposing)
        {
            ParentGame.Resources.Drop(textureId, texture);
            ParentGame.Resources.Drop(meshId, mesh);
        }
    }
}
