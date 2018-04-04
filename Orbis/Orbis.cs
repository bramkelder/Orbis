﻿using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Orbis.Engine;
using Orbis.Rendering;
using Orbis.Simulation;
using Orbis.States;
using Orbis.World;

using Orbis.UI.Windows;
using Orbis.UI.Elements;

namespace Orbis
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Orbis : Game
    {

        public static readonly int TEST_SEED = 0x55555555;
        public static readonly int TEST_CIVS = 15;
        public static readonly int TEST_RADIUS = 300;
        public static readonly int TEST_TICKS = 10000;

        public InputHandler Input { get { return InputHandler.GetInstance(); } }
        public GraphicsDeviceManager Graphics { get { return graphics; } }

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        InputHandler input;

        UI.UIManager UI;

        private Scene scene;
        private Simulator simulator;

        private SceneRendererComponent sceneRenderer;


        private SpriteFont fontDebug;

        float worldUpdateTimer;

        public Orbis()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            sceneRenderer = new SceneRendererComponent(this)
            {
                DrawOrder = 0
            };
            Components.Add(sceneRenderer);

            input = InputHandler.GetInstance();
            UI = new UI.UIManager(this)
            {
                DrawOrder = 1
            };
            Components.Add(UI);
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            AudioManager.Initialize();

            this.IsFixedTimeStep = false;
            graphics.SynchronizeWithVerticalRetrace = false;
            graphics.ApplyChanges();

            base.Initialize();
        }

        private void GenerateWorld(int seed, XMLModel.DecorationCollection decorationSettings, XMLModel.WorldSettings worldSettings, BiomeCollection biomeCollection, XMLModel.Civilization[] civSettings)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            // Generate world
            Debug.WriteLine("Generating world for seed " + seed);
            scene = new Scene(seed, worldSettings, decorationSettings, biomeCollection);
            var generator = new WorldGenerator(scene, civSettings);
            generator.GenerateWorld(TEST_RADIUS);
            generator.GenerateCivs(TEST_CIVS);

            simulator = new Simulator(scene, TEST_TICKS);

            stopwatch.Stop();
            Debug.WriteLine("Generated world in " + stopwatch.ElapsedMilliseconds + " ms");
            // Coloring data
            sceneRenderer.OnNewWorldGenerated(scene, seed);
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            AudioManager.LoadContent(Content);
            //Game Music
            //AudioManager.PlaySong("Crossing the Chasm");
            //AudioManager.PlaySong("Rocket");

            //Menu Music
            //AudioManager.PlaySong("Severe Tire Damage");

            XMLModel.DecorationCollection decorationSettings    = Content.Load<XMLModel.DecorationCollection>("Config/Decorations");
            XMLModel.WorldSettings worldSettings                = Content.Load<XMLModel.WorldSettings>("Config/WorldSettings");
            XMLModel.Civilization[] civSettings                 = Content.Load<XMLModel.Civilization[]>("Config/Civilization");
            fontDebug                                           = Content.Load<SpriteFont>("DebugFont");

            // Biome table test
            var biomeData = Content.Load<XMLModel.BiomeCollection>("Config/Biomes");
            var biomeCollection = new BiomeCollection(biomeData, Content);
            GenerateWorld(TEST_SEED, decorationSettings, worldSettings, biomeCollection, civSettings);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Update user input
            Input.UpdateInput();

            // Check if the state has changed
            if (StateManager.GetInstance().IsStateChanged())
            {
                StateManager.GetInstance().RunState();
            }

            // Update renderer if we can
            if (sceneRenderer.ReadyForUpdate)
            {
                simulator.Update(gameTime);
                Cell[] updatedCells = null;
                do
                {
                    updatedCells = simulator.GetChangedCells();
                    if (updatedCells != null && updatedCells.Length > 0)
                    {
                        sceneRenderer.UpdateScene(updatedCells);
                    }
                } while (updatedCells != null);
            }

            if (Input.IsKeyDown(Keys.S, new Keys[] { Keys.LeftShift, Keys.K, Keys.Y}))
            {
                Exit();
            }
            if (Input.IsKeyDown(Keys.P))
            {
                simulator.TogglePause();
            }

            TestWindow test = UI.CurrentWindow as TestWindow;
            test.TestBar.Progress = ((float)simulator.CurrentTick / TEST_TICKS);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            spriteBatch.Begin();

            spriteBatch.DrawString(fontDebug, "Tick: " + simulator.CurrentTick, new Vector2(10, 50), Color.Red);
            /*float y = 80;
            foreach (var civ in scene.Civilizations)
            {
                spriteBatch.DrawString(fontDebug, civ.Name, new Vector2(10, y), civ.Color);
                spriteBatch.DrawString(fontDebug, "Size: " + civ.Territory.Count, new Vector2(200, y), civ.Color);
                spriteBatch.DrawString(fontDebug, "Population: " + civ.Population, new Vector2(300, y), civ.Color);
                spriteBatch.DrawString(fontDebug, "Is Alive: " + civ.IsAlive, new Vector2(500, y), civ.Color);
                spriteBatch.DrawString(fontDebug, "Wealth: " + civ.TotalWealth, new Vector2(600, y), civ.Color);
                spriteBatch.DrawString(fontDebug, "Resource: " + civ.TotalResource, new Vector2(800, y), civ.Color);
                y += 15;
            }*/

            spriteBatch.DrawString(fontDebug, "FPS: " + (1 / gameTime.ElapsedGameTime.TotalSeconds).ToString("##.##"), new Vector2(10, 30), Color.Red);
            spriteBatch.DrawString(fontDebug, "Rendered Instances: " + sceneRenderer.RenderInstanceCount, new Vector2(100, 30), Color.Red);

            spriteBatch.End();
        }
    }
}
