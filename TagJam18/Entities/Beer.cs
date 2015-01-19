using System;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;

namespace TagJam18.Entities
{
    class Beer : Entity
    {
        private Model mesh;
        private const string meshId = "Beer/Mesh";
        private Texture2D texture;
        private const string textureId = "Beer/Texture";
        private Matrix baseTransform;
        private float size;
        private bool isDisappearing;
        private float disappearSpeed = 1f;

        [TilesetConstructor(2)]
        public Beer(Level level, int x, int y)
            : base(level.ParentGame)
        {
            this.Position = new Vector3((float)x, (float)y, 0f);
            mesh = ParentGame.Resources.Get<Model>(meshId, () => ParentGame.Content.Load<Model>("BeerBottle"));
            texture = ParentGame.Resources.Get<Texture2D>(textureId, () => ParentGame.Content.Load<Texture2D>("BeerBottleTexture"));

            const float idealRotation = -1f; // Has the label facing the camera for our default camera angle.
            float rotationVariance = MathF.Pi / 2f;
            float rotation = ParentGame.Random.NextFloat(-rotationVariance, rotationVariance) + idealRotation;

            const float positionVariance = 0.3f;
            float xOff = ParentGame.Random.NextFloat(-positionVariance, positionVariance);
            float yOff = ParentGame.Random.NextFloat(-positionVariance, positionVariance);
            Position += new Vector3(xOff, yOff, 0f);

            // Flip the model over * orient bottle * move bottom of bottle to proper bottom * make bottle smaller * apply random position offset
            baseTransform = Matrix.RotationX(MathF.Pi) * Matrix.RotationZ(rotation) * Matrix.Translation(0f, 0f, -1f);

            const float sizeVariance = 0.05f;
            size = 0.55f + ParentGame.Random.NextFloat(-sizeVariance, sizeVariance);
            CollisionSize = size;
        }

        public override void Render(GameTime gameTime)
        {
            ParentGame.BasicEffect.World = baseTransform * Matrix.Scaling(size) * Matrix.Translation(Position);
            ParentGame.BasicEffect.Texture = texture;
            ParentGame.BasicEffect.TextureEnabled = true;
            float oldSpecularPower = ParentGame.BasicEffect.SpecularPower;
            ParentGame.BasicEffect.SpecularPower = 64f;

            mesh.Draw(ParentGame.GraphicsDevice, ParentGame.BasicEffect.World, ParentGame.BasicEffect.View, ParentGame.BasicEffect.Projection, ParentGame.BasicEffect);

            ParentGame.BasicEffect.SpecularPower = oldSpecularPower;
            ParentGame.BasicEffect.TextureEnabled = false;
            ParentGame.BasicEffect.Texture = null;
        }

        public override void Update(GameTime gameTime)
        {
            if (!isDisappearing && CollidesWith(ParentGame.Player))
            {
                isDisappearing = true;
                ParentGame.Player.DrinkBeer();
            }

            if (isDisappearing)
            {
                size -= disappearSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;

                if (size < 0f)
                { this.Remove(); }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (Disposed)
            { return; }

            if (disposing)
            {
                ParentGame.Resources.Drop(textureId, texture);
                ParentGame.Resources.Drop(meshId, mesh);
            }
        }
    }
}
