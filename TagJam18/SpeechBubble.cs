using System;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;

namespace TagJam18
{
    public class SpeechBubble
    {
        private TagGame parentGame;
        private string message;
        private Vector2 messageSize;
        private Vector3 position;
        private float fade;
        private const float fadeSpeed = 0.25f;
        private SpriteFont font;
        private const string fontId = "SpeechBubble/Font";

        public SpeechBubble(TagGame parentGame, string message, Vector3 position)
        {
            this.parentGame = parentGame;
            this.message = message;
            this.position = position;
            this.fade = 0f;

            font = parentGame.Content.Load<SpriteFont>("LeagueGothic"); // Since TagGame uses this font too, we just let Toolkit cache it.
            messageSize = font.MeasureString(message);
        }

        public SpeechBubble(TagGame parentGame, string[] messages, Vector3 position)
            : this(parentGame, messages[parentGame.Random.Next(0, messages.Length)], position)
        { }
        
        public void Render(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (fade > 1f) // Just in case, don't render when fade > 100%
            { return; }

            const float floatHeight = 50f;
            const float disappearAfter = 0.5f;
            Vector3 protectedPosition = Vector3Ex.Project(position, parentGame.GraphicsDevice.Viewport, parentGame.BasicEffect.View * parentGame.BasicEffect.Projection);
            Vector2 screenPosition = new Vector2(protectedPosition.X, protectedPosition.Y - floatHeight * fade);
            Vector2 scale = Vector2.One;
            float alpha = 1f - MathF.Pow((fade - disappearAfter) / (1f - disappearAfter), 2f);
            if (fade > disappearAfter)
            { scale = new Vector2(1f, 1f - MathF.Pow((fade - disappearAfter) / (1f - disappearAfter), 2f)); }
            spriteBatch.DrawString(font, message, screenPosition, new Color(1f, 1f, 1f, alpha), 0f, messageSize / 2f, scale, SpriteEffects.None, 0f);
        }

        public void Update(GameTime gameTime)
        {
            fade += (float)gameTime.ElapsedGameTime.TotalSeconds * fadeSpeed;

            if (fade > 1f)
            { parentGame.RemoveSpeechBubble(this); }
        }
    }
}
