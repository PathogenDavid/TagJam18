﻿using System;
using SharpDX;
using SharpDX.Toolkit;

namespace TagJam18.Entities
{
    public class PlayerCamera : Entity
    {
        private Vector3 eye;
        private Vector3 at;
        private const float prefferredHeight = 30f;

        public Matrix ViewTransform { get; private set; }
        private Player Player
        {
            get { return ParentGame.Player; }
        }
        public Vector3 DefaultLookAt { get; set; }

        public PlayerCamera(TagGame parentGame)
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
