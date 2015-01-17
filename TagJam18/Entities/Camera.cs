using System;
using SharpDX;
using SharpDX.Toolkit;

namespace TagJam18.Entities
{
    public class Camera : Entity
    {
        private Vector3 eye;
        private Vector3 at;
        private const float prefferredHeight = 30f;

        public Matrix ViewTransform { get; private set; }
        public Player Player { get; set; }
        public Vector3 DefaultLookAt { get; set; }

        public Camera(TagGame parentGame)
            : base(parentGame)
        {
            eye = new Vector3(0f, 0f, -prefferredHeight);
            at = Vector3.Zero;
            ComputeViewTransform();
        }

        private void ComputeViewTransform()
        {
            ViewTransform = Matrix.LookAtRH(eye, at, -Vector3.UnitY);
        }

        public override void Update(GameTime gameTime)
        {
            if (Player == null)
            {
                eye = new Vector3(DefaultLookAt.X, DefaultLookAt.Y, -prefferredHeight);
                at = new Vector3(DefaultLookAt.X, DefaultLookAt.Y, 0f);
            }
            else
            {
                at = Player.Position;
                eye = Player.Position + new Vector3(0f, 0f, -prefferredHeight);
            }

            ComputeViewTransform();
        }
    }
}
