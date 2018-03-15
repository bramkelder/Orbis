﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Microsoft.Xna.Framework.Input;
using Orbis.Engine;
using Orbis.Rendering;
using Orbis.Simulation;
using Orbis.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Orbis
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Orbis : Game
    {
        public static readonly int TEST_SEED = 913279214;
        public static readonly int TEST_CIVS = 22;
        public static readonly int TEST_RADIUS = 128;
        public static readonly int TEST_TICKS = 10000;

        public InputHandler Input { get { return input; } }
        public GraphicsDeviceManager Graphics { get { return graphics; } }

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        InputHandler input;

        private Scene scene;
        private Simulator simulator;

        private SceneRendererComponent sceneRenderer;


        private SpriteFont fontDebug;

        float worldUpdateTimer;

        public Orbis()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            sceneRenderer = new SceneRendererComponent(this);
            Components.Add(sceneRenderer);

            input = new InputHandler();
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            this.IsFixedTimeStep = false;
            graphics.SynchronizeWithVerticalRetrace = false;
            graphics.ApplyChanges();

            base.Initialize();
        }

        private void GenerateWorld(int seed, XMLModel.WorldSettings worldSettings, BiomeCollection biomeCollection)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            // Generate world
            Debug.WriteLine("Generating world for seed " + seed);
            scene = new Scene(seed, worldSettings, biomeCollection);
            var generator = new WorldGenerator(scene);
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


            // Config Test
            XMLModel.Civilization[] civData = Content.Load<XMLModel.Civilization[]>("Config/Civilization");
            Debug.WriteLine(civData[0].name);
            Debug.WriteLine(civData[1].name);
            // End Config Test

            XMLModel.WorldSettings worldSettings = Content.Load<XMLModel.WorldSettings>("Config/WorldSettings");

            fontDebug = Content.Load<SpriteFont>("DebugFont");

            // Biome table test
            var biomeData = Content.Load<XMLModel.BiomeCollection>("Config/Biomes");
            var biomeCollection = new BiomeCollection(biomeData, Content);
            GenerateWorld(TEST_SEED, worldSettings, biomeCollection);
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
            input.UpdateInput();

            // Update renderer if we can
            if(sceneRenderer.ReadyForUpdate)
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

            if (input.IsKeyDown(Keys.S, new Keys[] { Keys.LeftShift, Keys.K, Keys.Y}))
            {
                Exit();
            }

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
            float y = 80;
            foreach(var civ in scene.Civilizations)
            {
                spriteBatch.DrawString(fontDebug, civ.Name + " - " + civ.Territory.Count + " - Population: " + civ.Population + " - Is Alive: " + civ.IsAlive, new Vector2(10, y), Color.Red);
                y += 15;
            }

            spriteBatch.DrawString(fontDebug, "FPS: " + (1 / gameTime.ElapsedGameTime.TotalSeconds).ToString("##.##"), new Vector2(10, 30), Color.Red);

            spriteBatch.End();
        }
    }
}
