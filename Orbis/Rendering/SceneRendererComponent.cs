﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Orbis.Simulation;
using Orbis.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Orbis.Rendering
{
    class SceneRendererComponent : DrawableGameComponent
    {
        /// <summary>
        /// Internal render data mapped to a cell
        /// </summary>
        class CellMappedData
        {
            public int meshIndex;
            public List<int> vertexIndexes;
        }

        struct MeshGenerationResult
        {
            public List<Mesh> rawMeshes;
            public List<RenderableMesh> renderableMeshes;
            public Dictionary<Cell, CellMappedData> cellData;
        }


        private Dictionary<Cell, CellMappedData> cellMappedData;
        private Orbis orbis;

        private Effect basicShader;
        private Texture2D black;
        private Dictionary<Civilization, Color> civColors;
        Camera camera;

        private List<RenderableMesh> cellMeshes;

        List<RenderInstance> renderInstances;

        private float rotation;
        private float distance;
        private float angle;
        private Model hexModel;
        private Model houseHexModel;
        private Model waterHexModel;

        private Queue<RenderableMesh> meshUpdateQueue;
        private Task<MeshGenerationResult> meshTask;
        private Scene renderedScene;

        public bool IsUpdatingMesh { get { return meshTask != null && meshTask.Status != TaskStatus.RanToCompletion; } }
        public bool ReadyForUpdate { get {
                return renderedScene != null && cellMappedData != null && cellMeshes != null && meshUpdateQueue.Count == 0;
            } }

        public float MaxUpdateTime { get; set; }

        public SceneRendererComponent(Orbis game) : base(game)
        {
            this.meshUpdateQueue = new Queue<RenderableMesh>();
            MaxUpdateTime = 3;
            this.orbis = game;
        }

        public override void Initialize()
        {
            // Camera stuff
            rotation = 0;
            distance = 20;
            angle = -60;

            camera = new Camera();
            //camera.Mode = CameraMode.Orthographic;

            renderInstances = new List<RenderInstance>();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            // Load shaders, set up shared settings
            black = Game.Content.Load<Texture2D>("black");
            basicShader = Game.Content.Load<Effect>("Shaders/BasicColorMapped");
            basicShader.CurrentTechnique = basicShader.Techniques["DefaultTechnique"];
            basicShader.Parameters["ColorMapTexture"].SetValue(black);

            var loader = new AtlasModelLoader(2048, 2048, basicShader, Game.Content);
            hexModel = loader.LoadModel("hex", "hex_grass", "hex_c");
            houseHexModel = loader.LoadModel("house", "house");
            waterHexModel = loader.LoadModel("hex", "hex_water", "hex_c");
            loader.FinializeLoading(GraphicsDevice);

            base.LoadContent();
        }

        protected override void UnloadContent()
        {
            base.UnloadContent();
        }

        public override void Update(GameTime gameTime)
        {
            // Check if new meshes have been generated by task
            if (meshTask != null && meshTask.Status == TaskStatus.RanToCompletion)
            {
                // ALWAYS dispose of RenderMesh objects that won't be used anymore to reclaim
                // the VertexBuffer and IndexBuffer memory they use
                // Currently none of them will be reused so we dispose all of them
                foreach (var instance in this.renderInstances)
                {
                    instance.mesh.Dispose();
                }
                var meshData = meshTask.Result;
                this.cellMappedData = meshData.cellData;
                this.cellMeshes = meshData.renderableMeshes;
                foreach(var mesh in this.cellMeshes)
                {
                    renderInstances.Add(new RenderInstance
                    {
                        mesh = mesh,
                        material = hexModel.Material,
                        matrix = Matrix.Identity
                    });
                }
                meshTask = null;
            }

            // TODO: Camera movement overhaul
            var camMoveDelta = Vector3.Zero;
            float speed = 100 * (float)gameTime.ElapsedGameTime.TotalSeconds;
            float scale = camera.OrthographicScale;

            if (orbis.Input.IsKeyHeld(Keys.LeftShift))
            {
                speed /= 5;
            }
            if (orbis.Input.IsKeyHeld(Keys.Up))
            {
                angle -= speed;
            }
            if (orbis.Input.IsKeyHeld(Keys.Down))
            {
                angle += speed;
            }
            if (orbis.Input.IsKeyHeld(Keys.Left))
            {
                rotation -= speed;
            }
            if (orbis.Input.IsKeyHeld(Keys.Right))
            {
                rotation += speed;
            }
            if (orbis.Input.IsKeyHeld(Keys.OemPlus))
            {
                distance -= speed;
                //scale -= speed;
            }
            if (orbis.Input.IsKeyHeld(Keys.OemMinus))
            {
                distance += speed;
                //scale += speed;
            }
            if (orbis.Input.IsKeyHeld(Keys.W))
            {
                camMoveDelta.Y += speed * 0.07f;
            }
            if (orbis.Input.IsKeyHeld(Keys.A))
            {
                camMoveDelta.X -= speed * 0.07f;
            }
            if (orbis.Input.IsKeyHeld(Keys.S))
            {
                camMoveDelta.Y -= speed * 0.07f;
            }
            if (orbis.Input.IsKeyHeld(Keys.D))
            {
                camMoveDelta.X += speed * 0.07f;
            }

            angle = MathHelper.Clamp(angle, -80, -5);
            distance = MathHelper.Clamp(distance, 1, 4000);

            //camera.OrthographicScale = MathHelper.Clamp(scale, 0.1f, 1000f);

            camera.LookTarget = camera.LookTarget + Vector3.Transform(camMoveDelta, Matrix.CreateRotationZ(MathHelper.ToRadians(rotation)));

            var camMatrix = Matrix.CreateTranslation(0, -distance, 0) *
               Matrix.CreateRotationX(MathHelper.ToRadians(angle)) *
               Matrix.CreateRotationZ(MathHelper.ToRadians(rotation));

            camera.Position = Vector3.Transform(Vector3.Zero, camMatrix) + camera.LookTarget;

            // Update some vertex buffers if we have to without impact framerate too much
            if(meshUpdateQueue.Count > 0)
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                do
                {
                    var mesh = meshUpdateQueue.Dequeue();
                    mesh.UpdateVertexBuffer(orbis.GraphicsDevice);
                } while (meshUpdateQueue.Count > 0 && stopwatch.Elapsed.TotalMilliseconds <= this.MaxUpdateTime);

                stopwatch.Stop();
                //Debug.WriteLine("Took " + stopwatch.Elapsed.TotalMilliseconds + " ms to update vertex buffers");
            }
            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            var graphics = orbis.Graphics;

            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            GraphicsDevice.Clear(Color.Aqua);

            // Required when using SpriteBatch as well
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.Opaque;

            float aspectRatio = orbis.Graphics.PreferredBackBufferWidth / (float)orbis.Graphics.PreferredBackBufferHeight;
            Matrix viewMatrix = camera.CreateViewMatrix();
            Matrix projectionMatrix = camera.CreateProjectionMatrix(aspectRatio);

            // Create batches sorted by material?
            var materialBatches = new Dictionary<Material, List<RenderInstance>>();
            foreach (var instance in renderInstances)
            {
                if (!materialBatches.ContainsKey(instance.material))
                {
                    materialBatches.Add(instance.material, new List<RenderInstance>());
                }
                materialBatches[instance.material].Add(instance);
            }

            // Draw batches
            foreach (var batch in materialBatches)
            {
                var effect = batch.Key.Effect;
                effect.Parameters["MainTexture"].SetValue(batch.Key.Texture);
                effect.Parameters["ColorMapTexture"].SetValue(batch.Key.ColorMap != null ? batch.Key.ColorMap : black);

                foreach (var instance in batch.Value)
                {
                    effect.Parameters["WorldViewProjection"].SetValue(instance.matrix * viewMatrix * projectionMatrix);

                    graphics.GraphicsDevice.Indices = instance.mesh.IndexBuffer;
                    graphics.GraphicsDevice.SetVertexBuffer(instance.mesh.VertexBuffer);
                    foreach (var pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();

                        graphics.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                            0,
                            0,
                            instance.mesh.IndexBuffer.IndexCount);
                    }
                }
            }

            base.Draw(gameTime);
        }

        /// <summary>
        /// Call when a new world is generated.
        /// </summary>
        /// <param name="scene">Scene containing new world</param>
        /// <param name="seed">Seed used to generate world</param>
        public async void OnNewWorldGenerated(Scene scene, int seed)
        {
            renderedScene = scene;

            var colorRandom = new Random(seed);
            civColors = new Dictionary<Civilization, Color>();
            foreach (var civ in scene.Civilizations)
            {
                civColors.Add(civ, new Color(colorRandom.Next(256), colorRandom.Next(256), colorRandom.Next(256)));
            }


            // Await for a previous mesh generation to finish if it hasn't yet
            if (meshTask != null)
            {
                await meshTask;
            }
            meshTask = Task.Run(() => {
                return GenerateMeshesFromScene(scene);
            });

            // Set cam to sea level
            camera.LookTarget = new Vector3(camera.LookTarget.X, camera.LookTarget.Y, scene.WorldMap.SeaLevel);
        }

        private MeshGenerationResult GenerateMeshesFromScene(Scene scene)
        {
            var renderableMeshes = new List<RenderableMesh>();
            var rawMeshes = new List<Mesh>();
            var cellData = new Dictionary<Cell, CellMappedData>();

            // Hex generation test
            var hexMesh = hexModel.Mesh;
            var houseHexMesh = houseHexModel.Mesh;
            var waterHexMesh = waterHexModel.Mesh;
            // Use mesh combiners to get a bit more performant mesh for now
            var hexCombiner = new MeshCombiner();

            // Create world meshes
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            int range = scene.WorldMap.Radius;

            for (int p = -range; p <= range; p++)
            {
                for (int q = -range; q <= range; q++)
                {
                    var cell = scene.WorldMap.GetCell(p, q);
                    if (cell == null)
                    {
                        continue;
                    }

                    var worldPoint = TopographyHelper.HexToWorld(new Point(p, q));
                    var position = new Vector3(
                        worldPoint,
                        (float)cell.Elevation);
                    // Cell color
                    // TODO: This doesn't work because the combiner doesn't combine immediately. Ensure that it does or add color to MeshInstance?
                    var color = GetCellColor(cell);
                    var mesh = cell.IsWater ? waterHexMesh : hexMesh;

                    // Temporary way to make sea actually level
                    if (cell.IsWater)
                    {
                        position.Z = scene.WorldMap.SeaLevel;
                    }

                    int meshIndex = hexCombiner.Add(new MeshInstance
                    {
                        mesh = mesh,
                        matrix = Matrix.CreateTranslation(position),
                        pos = new Point(p, q),
                        color = color,
                        useColor = true,
                    });

                    // Register partial cell mapped data
                    cellData[cell] = new CellMappedData
                    {
                        meshIndex = meshIndex
                    };
                }
            }

            // Combine meshes
            var meshList = hexCombiner.GetCombinedMeshes();
            for(int i = 0; i < meshList.Count; i++)
            {
                var renderable = new RenderableMesh(orbis.GraphicsDevice, meshList[i]);
                renderableMeshes.Add(renderable);

                Debug.WriteLine("Adding hex mesh");
            }
            // Finish cell mapped data
            foreach(var cell in cellData)
            {
                var mesh = meshList[cell.Value.meshIndex];
                cell.Value.vertexIndexes = mesh.TagIndexMap[cell.Key.Coordinates];
            }

            stopwatch.Stop();
            Debug.WriteLine("Generated " + renderableMeshes.Count + " meshes in " + stopwatch.ElapsedMilliseconds + " ms");

            return new MeshGenerationResult {
                cellData = cellData,
                rawMeshes = rawMeshes,
                renderableMeshes = renderableMeshes
            };
        }

        /// <summary>
        /// Called to update the rendered representation of the scene.
        /// MAY NOT be put in a task.
        /// </summary>
        /// <param name="scene">Scene to update to</param>
        public void UpdateScene(Cell[] cells)
        {
            if(!ReadyForUpdate)
            {
                throw new Exception("Not ready for update yet!");
            }

            meshUpdateQueue.Clear();
            int updatedCells = 0;
            //var stopwatch = new Stopwatch();
            //stopwatch.Start();

            var updatedMeshes = new HashSet<RenderableMesh>();
            foreach(var cell in cells)
            {
                if(cell == null) { continue; }
                var data = cellMappedData[cell];
                var mesh = cellMeshes[data.meshIndex];
                foreach(var i in data.vertexIndexes)
                {
                    mesh.VertexData[i].Color = GetCellColor(cell);
                }
                updatedMeshes.Add(mesh);
                updatedCells++;
            }

            //stopwatch.Stop();
            //Debug.WriteLine("Took " + stopwatch.ElapsedMilliseconds + " ms to update vertexdata for " + updatedCells + " cells");
            foreach(var mesh in updatedMeshes)
            {
                meshUpdateQueue.Enqueue(mesh);
            }
        }

        private Color GetCellColor(Cell cell)
        {
            var color = cell.Owner != null ? civColors[cell.Owner] : cell.IsWater ? Color.Aquamarine : Color.Black;
            return color;
        }
    }
}
