﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Orbis.Engine;
using System;

namespace Orbis.UI.BasicElements
{
    /// <summary>
    ///     Represents a scrollbar that can be used by UI Elements.
    /// </summary>
    public class Scrollbar : IRenderableElement, IUpdatableElement
    {
        // Used to display the background.
        private PositionedTexture _background;

        // Used for moving the scrollbar down.
        private Button _downButton;

        // Used to display the handle;
        private PositionedTexture _handle;

        // Used for moving the scrollbar up.
        private Button _upButton;

        /// <summary>
        ///     The bounds of the scrollbar.
        /// </summary>
        public Rectangle Bounds
        {
            get
            {
                return new Rectangle(Position, Size);
            }
        }

        /// <summary>
        ///     The layer depth of the scrollbar.
        /// </summary>
        /// 
        /// <remarks>
        ///     Value should be between 0 and 1, with zero being the front and 1 being the back.
        /// </remarks>
        /// 
        /// <exception cref="ArgumentOutOfRangeException" />
        public float LayerDepth
        {
            get
            {
                return _background.LayerDepth;
            }
            set
            {
                _background.LayerDepth = value;
                _upButton.LayerDepth = value;
                _downButton.LayerDepth = value;

                // The handle is drawn slightly above the background.
                _handle.LayerDepth = value - 0.001F;
            }
        }

        /// <summary>
        ///     The position of the scrollbar.
        /// </summary>
        public Point Position
        {
            get
            {
                return _background.Position + new Point(0, -15);
            }
            set
            {
                _background.Position = value + new Point(0, 15);

                // Encapsulated into functions because of reuse.
                UpdateButtonPositions();
                UpdateHandlePosition();
            }
        }

        /// <summary>
        ///     The on-screen area that contains the scrollbar.
        /// </summary>
        public Rectangle ScreenArea
        {
            get
            {
                return _screenArea;
            }
            set
            {
                _screenArea = value;

                _upButton.ScreenArea = new Rectangle(value.X, value.Y, value.Width, 15);
                _downButton.ScreenArea = new Rectangle(value.Left, value.Bottom - 15, value.Width, 15);
            }
        }
        private Rectangle _screenArea;

        /// <summary>
        ///     The relative value of the handle's position within the scrollbar.
        /// </summary>
        /// 
        /// <remarks>
        ///     The scroll position is limited to the range 0 - 100.
        /// </remarks>
        public float ScrollPosition
        {
            get
            {
                return _scrollPos;
            }
            set
            {
                // Restrict value between 0 and 100.
                _scrollPos = MathHelper.Clamp(value, 0F, 100F);
            }
        }
        private float _scrollPos;

        /// <summary>
        ///     The size of the scrollbar.
        /// </summary>
        public Point Size
        {
            get
            {
                return _background.Size + new Point(0, 30);
            }
            set
            {
                _background.Size = (value.X == 15) ? value + new Point(0, -30)
                    : throw new ArgumentOutOfRangeException("Scrollbar can not be wider than 15px.");

                UpdateButtonPositions();
                UpdateHandlePosition();
            }
        }

        /// <summary>
        ///     Create a new <see cref="Scrollbar"/>.
        /// </summary>
        /// 
        /// <exception cref="Exception" />
        public Scrollbar()
        {   
            // Before the scrollbar items can be created, the required resources need to be created.
            Texture2D scrollbarSheet;
            SpriteFont defaultFont;
            if (UIContentManager.TryGetInstance(out UIContentManager manager))
            {
                scrollbarSheet = manager.GetTexture("UI\\Scrollbar");
                defaultFont = manager.GetFont("DebugFont");
            }
            else
            {
                throw new Exception("Could not retrieve scrollbar spritesheet.");
            }

            // Sprite definitions are created for the different sprites used by scrollbar items.
            Rectangle buttonRect = new Rectangle(0, 0, 15, 15);
            SpriteDefinition buttonDef = new SpriteDefinition(scrollbarSheet, buttonRect);

            Rectangle handleRect = new Rectangle(0, 15, 15, 20);
            SpriteDefinition handleDef = new SpriteDefinition(scrollbarSheet, handleRect);

            Rectangle backgroundRect = new Rectangle(15, 0, 15, 35);
            SpriteDefinition backgroundDef = new SpriteDefinition(scrollbarSheet, backgroundRect);

            // Finally, the items themselves can be created.
            _upButton = new Button(buttonDef, defaultFont)
            {
                Size = new Point(15, 15)
            };
            _downButton = new Button(buttonDef, defaultFont)
            {
                Size = new Point(15, 15),
                // Down button uses same texture but flipped.
                SpriteEffects = SpriteEffects.FlipVertically
            };
            _handle = new PositionedTexture(handleDef)
            {
                Size = new Point(15, 20)
            };
            _background = new PositionedTexture(backgroundDef);

            _downButton.Hold += _downButton_Hold;
            _upButton.Hold += _upButton_Hold;

            ScrollPosition = 0F;
        }

        /// <summary>
        ///     Update the position of the buttons.
        /// </summary>
        /// 
        /// <remarks>
        ///     Encapsulated into a function because of reuse.
        /// </remarks>
        private void UpdateButtonPositions()
        {
            _upButton.Position = Position;
            _downButton.Position = Position + new Point(0, Size.Y - 15);
        }

        /// <summary>
        ///     Update the position of the handle.
        /// </summary>
        /// 
        /// <remarks>
        ///     Encapsulated into a function because of reuse.
        /// </remarks>
        private void UpdateHandlePosition()
        {
            // Handle is positioned at the rounded relative value of the scroll position.
            _handle.Position = Position + new Point(0, (int)Math.Floor((_background.Size.Y - 20) * (ScrollPosition / 100)) + 15);
        }

        /// <summary>
        ///     Event handler for holding the mouse down over the down button.
        /// </summary>
        private void _downButton_Hold(object sender, EventArgs e)
        {
            ScrollPosition = MathHelper.Clamp(ScrollPosition + 1F, 0F, 100F);

            UpdateHandlePosition();
        }

        /// <summary>
        ///     Event handler for holding the mouse down over the up button.
        /// </summary>
        private void _upButton_Hold(object sender, EventArgs e)
        {
            ScrollPosition = MathHelper.Clamp(ScrollPosition - 1F, 0F, 100F);

            UpdateHandlePosition();
        }

        /// <summary>
        ///     Perform the update for this frame.
        /// </summary>
        public void Update()
        {
            // Updating the buttons is only necessary when the mouse is over the scrollbar.
            Point mousePos = Mouse.GetState().Position;
            if (ScreenArea.Contains(mousePos))
            {
                _upButton.Update();
                _downButton.Update();
            }
        }

        /// <summary>
        ///     Render the scrollbar using the given spriteBatch.
        /// </summary>
        /// 
        /// <param name="spriteBatch">
        ///     The spriteBatch to use for drawing;
        /// </param>
        public void Render(SpriteBatch spriteBatch)
        {
            _upButton.Render(spriteBatch);
            _background.Render(spriteBatch);
            _downButton.Render(spriteBatch);
            _handle.Render(spriteBatch);
        }
    }
}
