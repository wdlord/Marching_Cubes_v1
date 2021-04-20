// --------------------------------------------------------------------------------------------------------------------------------------------
// Marching Cubes
// The algorithm is essentially a 3D graphing function. Here are my main resources for this project: 
// NVidia GPU Gems 3: https://developer.nvidia.com/gpugems/gpugems3/part-i-geometry/chapter-1-generating-complex-procedural-terrains-using-gpu
// Paul Borke: http://paulbourke.net/geometry/polygonise/
// In the future I would like to do some work on the GPU side like in the NVidia example, and perhaps rebuild the project in Unity.
// --------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Marching_Cubes
{
    public class MainClass : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        // added everything below this line
        Effect effect;
        BasicEffect basicEffect;
        Matrix world = Matrix.CreateTranslation(0, 0, 0);
        Matrix view = Matrix.CreateLookAt(new Vector3(0, 0, 3), new Vector3(0, 0, 0), new Vector3(0, 1, 0));
        Matrix projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45), 800f / 480f, 0.01f, 100f);

        // marching cubes specific variables
        VertexBuffer vertexBuffer;
        VertexBuffer[,,] buffers;   // don't remove
        int bufferIndex;
        int vBufSize;
        VertexPositionColor[] vertices;
        Voxel[,,] voxels;
        int[][] triangleTable;
        bool showVoxels;

        // cube vertices
        Vector3 v0 = new Vector3(1, 1, 0);
        Vector3 v1 = new Vector3(1, 0, 0);
        Vector3 v2 = new Vector3(0, 0, 0);
        Vector3 v3 = new Vector3(0, 1, 0);
        Vector3 v4 = new Vector3(1, 1, 1);
        Vector3 v5 = new Vector3(1, 0, 1);
        Vector3 v6 = new Vector3(0, 0, 1);
        Vector3 v7 = new Vector3(0, 1, 1);

        // camera stuff
        float yaw;
        float pitch;    // unused
        float roll;
        Vector3 CameraPosition;
        Vector3 CameraTarget = new Vector3(16, 16, 16);
        float distance = 40;
        MouseState previousMouseState;

        // update method stuff
        KeyboardState preKeyboard;
        int function;

        // UX
        bool showHelp;
        int lineNum;
        SpriteFont font;

        public MainClass()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            //***************************
            _graphics.GraphicsProfile = GraphicsProfile.HiDef;
            //***************************
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            showHelp = true;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // added everything below this line
            font = Content.Load<SpriteFont>("Arial");
            basicEffect = new BasicEffect(GraphicsDevice);
            //effect = Content.Load<Effect>("Shader");

            // non-repeating voxel stuff
            LoadTable();
            vBufSize = 3 * 12 * (32 * 32 * 32);
            voxels = new Voxel[32, 32, 32];
            buffers = new VertexBuffer[32, 32, 32];

            RunAlgorithm();
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // ----------------- camera functionality
            CameraFunctionality();


            // ----------------- function selection
            if (Keyboard.GetState().IsKeyDown(Keys.D1) && !preKeyboard.IsKeyDown(Keys.D1))
            {
                if (function != 0)
                {
                    function = 0;
                    RunAlgorithm();
                }
            }
            if (Keyboard.GetState().IsKeyDown(Keys.D2) && !preKeyboard.IsKeyDown(Keys.D2))
            {
                if (function != 1)
                {
                    function = 1;
                    RunAlgorithm();
                }
            }
            if (Keyboard.GetState().IsKeyDown(Keys.D3) && !preKeyboard.IsKeyDown(Keys.D3))
            {
                if (function != 2)
                {
                    function = 2;
                    RunAlgorithm();
                }
            }
            if (Keyboard.GetState().IsKeyDown(Keys.D4) && !preKeyboard.IsKeyDown(Keys.D4))
            {
                if (function != 3)
                {
                    function = 3;
                    RunAlgorithm();
                }
            }

            // ----------------- toggle showing voxel space
            if (Keyboard.GetState().IsKeyDown(Keys.V) && !preKeyboard.IsKeyDown(Keys.V))
            {
                showVoxels = !showVoxels;
                vertices = null;
                RunAlgorithm();
            }

            // ----------------- help/debug menu toggle
            if (Keyboard.GetState().IsKeyDown(Keys.OemQuestion) && !preKeyboard.IsKeyDown(Keys.OemQuestion))
            {
                showHelp = !showHelp;
            }

            preKeyboard = Keyboard.GetState();
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here
            basicEffect.World = world;
            basicEffect.View = view;
            basicEffect.Projection = projection;
            basicEffect.VertexColorEnabled = true;

            GraphicsDevice.SetVertexBuffer(vertexBuffer);

            RasterizerState rasterizerState = new RasterizerState();
            rasterizerState.CullMode = CullMode.None;                   // THIS IS OPTIONAL
            GraphicsDevice.RasterizerState = rasterizerState;

            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, vBufSize / 3);
            }

            if (showHelp) HelpMenu();

            base.Draw(gameTime);
        }

        // CUSTOM FUNCTIONS
        void CameraFunctionality()
        {
            // user input adjustments
            MouseState currentMouseState = Mouse.GetState();

            if (currentMouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Pressed)
            {
                yaw += (previousMouseState.X - currentMouseState.X) / 100f;
                roll += (previousMouseState.Y - currentMouseState.Y) / 100f;
            }

            if (currentMouseState.RightButton == ButtonState.Pressed && previousMouseState.RightButton == ButtonState.Pressed)
            {
                distance += 2*(previousMouseState.Y - currentMouseState.Y) / 100f;
            }
            previousMouseState = Mouse.GetState();

            // matrix adjustments
            CameraPosition = Vector3.Transform(Vector3.Backward, Matrix.CreateFromYawPitchRoll(yaw, roll, 0));
            CameraPosition *= distance;
            CameraPosition += CameraTarget;

            view = Matrix.CreateLookAt(CameraPosition, CameraTarget, Vector3.Up);
        }
        // used to make it easier for me to write menu info to the screen
        private void Write(String str)
        {
            _spriteBatch.DrawString(font, str, new Vector2(10, lineNum * 20), Color.White);
            lineNum++;
        }

        // draws the help menu
        private void HelpMenu()
        {
            _spriteBatch.Begin();

            Write("Help Screen");
            Write("Rotate Camera: Left MB");
            Write("Zoom: Right MB");
            Write("Reset Camera: S");
            Write("Function Previews: 1, 2, 3, 4");
            Write("Toggle Voxel Visibility: V");
            Write("");

            string currentFunction = "";
            switch (function)
            {
                case 0:
                    currentFunction = "z = 2* sin(x) + 2 * sin(y)";
                    break;
                case 1:
                    currentFunction = "z = 0.1(x^2) * 0.1(y^2)";
                    break;
                case 2:
                    currentFunction = "z = 0.5(x-y) * sin(z)";
                    break;
                case 3:
                    currentFunction = "z = x*cos(y*x)";
                    break;
            }
            Write(String.Format("Current Function: {0}", currentFunction));

            lineNum = 0;
            _spriteBatch.End();

            // necessary because spriteBatch.Begin() resets to it's preferred states
            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        }

        void RunAlgorithm()
        {
            // trying to save memory
            vertices = null;
            vertexBuffer = null;

            // voxel stuff
            EvaluateFunction();

            // create our 32x32x32 block
            vertices = new VertexPositionColor[vBufSize];

            // run algorithm and set vertex buffer
            MarchCubes();
            vertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionColor), vertices.Length, BufferUsage.WriteOnly);
            vertexBuffer.SetData<VertexPositionColor>(vertices);
        }

        // function to render the entire block
        void MarchCubes()
        {
            int dim = 32;
            bufferIndex = 0;
            for (int i = 0; i < dim; i++)
            {
                for (int j = 0; j < dim; j++)
                {
                    for (int k = 0; k < dim; k++)
                    {
                        // only draw vertices that make the surface of the geometry
                        if (voxels[i, j, k].VertexCase != 0 && voxels[i, j, k].VertexCase != 255)
                        {
                            if (showVoxels) BuildCube(i, j, k);
                            else InterpretCase(i, j, k);
                        }
                    }
                }
            }
        }

        // creates a voxel at a given position
        void BuildCube(int x, int y, int z)
        {
            // face 1
            vertices[bufferIndex + 0] = new VertexPositionColor(new Vector3(x + 0, y + 1, z + 0), Color.Red);
            vertices[bufferIndex + 1] = new VertexPositionColor(new Vector3(x + 0, y + 0, z + 1), Color.Blue);
            vertices[bufferIndex + 2] = new VertexPositionColor(new Vector3(x + 0, y + 0, z + 0), Color.Green);

            vertices[bufferIndex + 3] = new VertexPositionColor(new Vector3(x + 0, y + 1, z + 0), Color.Red);
            vertices[bufferIndex + 4] = new VertexPositionColor(new Vector3(x + 0, y + 0, z + 1), Color.Blue);
            vertices[bufferIndex + 5] = new VertexPositionColor(new Vector3(x + 0, y + 1, z + 1), Color.Yellow);

            // face 2
            vertices[bufferIndex + 6] = new VertexPositionColor(new Vector3(x + 1, y + 1, z + 0), Color.Yellow);
            vertices[bufferIndex + 7] = new VertexPositionColor(new Vector3(x + 1, y + 0, z + 1), Color.Green);
            vertices[bufferIndex + 8] = new VertexPositionColor(new Vector3(x + 1, y + 0, z + 0), Color.Red);

            vertices[bufferIndex + 9] = new VertexPositionColor(new Vector3(x + 1, y + 1, z + 0), Color.Yellow);
            vertices[bufferIndex + 10] = new VertexPositionColor(new Vector3(x + 1, y + 0, z + 1), Color.Green);
            vertices[bufferIndex + 11] = new VertexPositionColor(new Vector3(x + 1, y + 1, z + 1), Color.Blue);

            // face 3
            vertices[bufferIndex + 12] = new VertexPositionColor(new Vector3(x + 0, y + 0, z + 0), Color.Green);
            vertices[bufferIndex + 13] = new VertexPositionColor(new Vector3(x + 1, y + 0, z + 1), Color.Green);
            vertices[bufferIndex + 14] = new VertexPositionColor(new Vector3(x + 0, y + 0, z + 1), Color.Blue);

            vertices[bufferIndex + 15] = new VertexPositionColor(new Vector3(x + 0, y + 0, z + 0), Color.Green);
            vertices[bufferIndex + 16] = new VertexPositionColor(new Vector3(x + 1, y + 0, z + 1), Color.Green);
            vertices[bufferIndex + 17] = new VertexPositionColor(new Vector3(x + 1, y + 0, z + 0), Color.Red);

            // face 4
            vertices[bufferIndex + 18] = new VertexPositionColor(new Vector3(x + 0, y + 1, z + 0), Color.Red);
            vertices[bufferIndex + 19] = new VertexPositionColor(new Vector3(x + 1, y + 1, z + 1), Color.Blue);
            vertices[bufferIndex + 20] = new VertexPositionColor(new Vector3(x + 0, y + 1, z + 1), Color.Yellow);

            vertices[bufferIndex + 21] = new VertexPositionColor(new Vector3(x + 0, y + 1, z + 0), Color.Red);
            vertices[bufferIndex + 22] = new VertexPositionColor(new Vector3(x + 1, y + 1, z + 1), Color.Blue);
            vertices[bufferIndex + 23] = new VertexPositionColor(new Vector3(x + 1, y + 1, z + 0), Color.Red);

            // face 5
            vertices[bufferIndex + 24] = new VertexPositionColor(new Vector3(x + 0, y + 0, z + 0), Color.Green);
            vertices[bufferIndex + 25] = new VertexPositionColor(new Vector3(x + 1, y + 1, z + 0), Color.Red);
            vertices[bufferIndex + 26] = new VertexPositionColor(new Vector3(x + 0, y + 1, z + 0), Color.Red);

            vertices[bufferIndex + 27] = new VertexPositionColor(new Vector3(x + 0, y + 0, z + 0), Color.Green);
            vertices[bufferIndex + 28] = new VertexPositionColor(new Vector3(x + 1, y + 1, z + 0), Color.Red);
            vertices[bufferIndex + 29] = new VertexPositionColor(new Vector3(x + 1, y + 0, z + 0), Color.Blue);

            // face 6
            vertices[bufferIndex + 30] = new VertexPositionColor(new Vector3(x + 0, y + 0, z + 1), Color.Blue);
            vertices[bufferIndex + 31] = new VertexPositionColor(new Vector3(x + 1, y + 1, z + 1), Color.Blue);
            vertices[bufferIndex + 32] = new VertexPositionColor(new Vector3(x + 0, y + 1, z + 1), Color.Yellow);

            vertices[bufferIndex + 33] = new VertexPositionColor(new Vector3(x + 0, y + 0, z + 1), Color.Blue);
            vertices[bufferIndex + 34] = new VertexPositionColor(new Vector3(x + 1, y + 1, z + 1), Color.Blue);
            vertices[bufferIndex + 35] = new VertexPositionColor(new Vector3(x + 1, y + 0, z + 1), Color.Green);

            bufferIndex = bufferIndex + 36;
        }

        // this is used as a sample for marching cubes generation
        float SampleSlope(Vector3 coord)
        {
            double output = 0;
            coord -= Vector3.One * 16;  // adjust the coordinate plane to be centered at (0, 0, 0) instead of (16, 16, 16)

            switch(function)
            {
                case 0:
                    output = 2 * Math.Sin(coord.X) + 2 * Math.Sin(coord.Y);
                    break;
                case 1:
                    output = 0.1 * Math.Pow(coord.X, 2) * 0.1 * Math.Pow(coord.Y, 2);
                    break;
                case 2:
                    output = 0.5 * (coord.X - coord.Y) * Math.Sin(coord.Z);
                    break;
                case 3:
                    output = coord.X * Math.Cos(coord.X * coord.Y);
                    break;
            }

            return (float)output - coord.Z;
        }

        // represents an arbitrary voxel
        struct Voxel
        {
            public Voxel(int VCase)
            {
                VertexCase = VCase;
            }
            public int VertexCase { get; }
        }


        // evaluates the function for every voxel
        void EvaluateFunction()
        {
            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    for (int z = 0; z < 32; z++)
                    {
                        EvaluateVoxel(x, y, z);
                    }
                }
            }
        }

        // evaluates the function for a given voxel
        void EvaluateVoxel(int x, int y, int z)
        {
            Vector3 coordinate = new Vector3(x, y, z);

            // evaluate all 8 corners of the voxel against the sample function
            int a = SampleSlope(coordinate + v0) > 0 ? 1 : 0;
            int b = SampleSlope(coordinate + v1) > 0 ? 1 : 0;
            int c = SampleSlope(coordinate + v2) > 0 ? 1 : 0;
            int d = SampleSlope(coordinate + v3) > 0 ? 1 : 0;
            int e = SampleSlope(coordinate + v4) > 0 ? 1 : 0;
            int f = SampleSlope(coordinate + v5) > 0 ? 1 : 0;
            int g = SampleSlope(coordinate + v6) > 0 ? 1 : 0;
            int h = SampleSlope(coordinate + v7) > 0 ? 1 : 0;

            // calculate the case number by concatenating the binary values
            int VCase = 1 * a + 2 * b + 4 * c + 8 * d + 16 * e + 32 * f + 64 * g + 128 * h;

            // set the values in the voxel struct
            voxels[x, y, z] = new Voxel(VCase);
        }

        void InterpretCase(int x, int y, int z)
        {
            int voxelCase = voxels[x, y, z].VertexCase;
            
            // generate the vertices for given voxel case
            for (int i = 0; triangleTable[voxelCase][i] != -1; i+=3)
            {
                vertices[bufferIndex + 0] = MakeVertex(triangleTable[voxelCase][i + 2], x, y, z, Color.Red);
                vertices[bufferIndex + 1] = MakeVertex(triangleTable[voxelCase][i + 1], x, y, z, Color.Green);
                vertices[bufferIndex + 2] = MakeVertex(triangleTable[voxelCase][i + 0], x, y, z, Color.Blue);

                bufferIndex += 3;
            }
        }

        // returns the correct vertex in space based off of given vertex input and position in space
        VertexPositionColor MakeVertex(int edge, int x, int y, int z, Color color)
        {
            Vector3 output = Vector3.Zero;
            Vector3 coord = new Vector3(x, y, z);
            float point1, point2;

            // determine where to put the vertex along the edge
            switch (edge)
            {
                case 0:
                    point1 = SampleSlope(coord + v0);
                    point2 = SampleSlope(coord + v1);
                    output = Interpolate(point1, point2, coord + v0, coord + v1);
                    break;
                case 1:
                    point1 = SampleSlope(coord + v1);
                    point2 = SampleSlope(coord + v2);
                    output = Interpolate(point1, point2, coord + v1, coord + v2);
                    break;
                case 2:
                    point1 = SampleSlope(coord + v2);
                    point2 = SampleSlope(coord + v3);
                    output = Interpolate(point1, point2, coord + v2, coord + v3);
                    break;
                case 3:
                    point1 = SampleSlope(coord + v0);
                    point2 = SampleSlope(coord + v3);
                    output = Interpolate(point1, point2, coord + v0, coord + v3);
                    break;
                case 4:
                    point1 = SampleSlope(coord + v4);
                    point2 = SampleSlope(coord + v5);
                    output = Interpolate(point1, point2, coord + v4, coord + v5);
                    break;
                case 5:
                    point1 = SampleSlope(coord + v5);
                    point2 = SampleSlope(coord + v6);
                    output = Interpolate(point1, point2, coord + v5, coord + v6);
                    break;
                case 6:
                    point1 = SampleSlope(coord + v6);
                    point2 = SampleSlope(coord + v7);
                    output = Interpolate(point1, point2, coord + v6, coord + v7);
                    break;
                case 7:
                    point1 = SampleSlope(coord + v4);
                    point2 = SampleSlope(coord + v7);
                    output = Interpolate(point1, point2, coord + v4, coord + v7);
                    break;
                case 8:
                    point1 = SampleSlope(coord + v0);
                    point2 = SampleSlope(coord + v4);
                    output = Interpolate(point1, point2, coord + v0, coord + v4);
                    break;
                case 9:
                    point1 = SampleSlope(coord + v1);
                    point2 = SampleSlope(coord + v5);
                    output = Interpolate(point1, point2, coord + v1, coord + v5);
                    break;
                case 10:
                    point1 = SampleSlope(coord + v2);
                    point2 = SampleSlope(coord + v6);
                    output = Interpolate(point1, point2, coord + v2, coord + v6);
                    break;
                case 11:
                    point1 = SampleSlope(coord + v7);
                    point2 = SampleSlope(coord + v3);
                    output = Interpolate(point1, point2, coord + v7, coord + v3);
                    break;
            }

            return new VertexPositionColor(output, color);
        }


        Vector3 Interpolate(float point1, float point2, Vector3 vertexA, Vector3 vertexB)
        {
            Vector3 output = Vector3.Zero;
            float fraction, interpolation, a=0, b=0;
            int coordCase = 0;

            // find which coordinates need to be interpolated (x, y, or z coords)
            if (vertexA.X != vertexB.X)
            {
                a = vertexA.X;
                b = vertexB.X;
                coordCase = 1;
            }
            else if (vertexA.Y != vertexB.Y)
            {
                a = vertexA.Y;
                b = vertexB.Y;
                coordCase = 2;
            }
            else if (vertexA.Z != vertexB.Z)
            {
                a = vertexA.Z;
                b = vertexB.Z;
                coordCase = 3;
            }

            // the fraction represents the distance to place the vertex between a and b (ie 25% of the way between A and B)
            float numerator = (point1 >= 0) ? point1 : point2;
            fraction = numerator / (Math.Abs(point1) + Math.Abs(point2));

            // swap variables 'a' and 'b', not doing so results in innaccuracy (ie 25% of the way between B and A instead of A and B)
            if (point1 >= 0) interpolation = (float) (a * (1.0 - fraction)) + (b * fraction);
            else interpolation = (float)(b * (1.0 - fraction)) + (a * fraction);

            if (coordCase == 1) output = new Vector3(interpolation, vertexA.Y, vertexB.Z);
            if (coordCase == 2) output = new Vector3(vertexA.X, interpolation, vertexB.Z);
            if (coordCase == 3) output = new Vector3(vertexA.X, vertexA.Y, interpolation);

            return output;
        }


        // geometry table for vertex cases. -1 means no vertex, other represent which edge to interpolate.
        void LoadTable()
        {
            triangleTable = new int[256][]
                {
                new[] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 0, 1, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 1, 8, 3, 9, 8, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 0, 8, 3, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 9, 2, 10, 0, 2, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 2, 8, 3, 2, 10, 8, 10, 9, 8, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 0, 11, 2, 8, 11, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 1, 9, 0, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 1, 11, 2, 1, 9, 11, 9, 8, 11, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 3, 10, 1, 11, 10, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 0, 10, 1, 0, 8, 10, 8, 11, 10, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 3, 9, 0, 3, 11, 9, 11, 10, 9, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 9, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 4, 3, 0, 7, 3, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 0, 1, 9, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 4, 1, 9, 4, 7, 1, 7, 3, 1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 1, 2, 10, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 3, 4, 7, 3, 0, 4, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 9, 2, 10, 9, 0, 2, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 2, 10, 9, 2, 9, 7, 2, 7, 3, 7, 9, 4, -1, -1, -1, -1 },
                new[] { 8, 4, 7, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 11, 4, 7, 11, 2, 4, 2, 0, 4, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 9, 0, 1, 8, 4, 7, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 4, 7, 11, 9, 4, 11, 9, 11, 2, 9, 2, 1, -1, -1, -1, -1 },
                new[] { 3, 10, 1, 3, 11, 10, 7, 8, 4, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 1, 11, 10, 1, 4, 11, 1, 0, 4, 7, 11, 4, -1, -1, -1, -1 },
                new[] { 4, 7, 8, 9, 0, 11, 9, 11, 10, 11, 0, 3, -1, -1, -1, -1 },
                new[] { 4, 7, 11, 4, 11, 9, 9, 11, 10, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 9, 5, 4, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 0, 5, 4, 1, 5, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 8, 5, 4, 8, 3, 5, 3, 1, 5, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 1, 2, 10, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 3, 0, 8, 1, 2, 10, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 5, 2, 10, 5, 4, 2, 4, 0, 2, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 2, 10, 5, 3, 2, 5, 3, 5, 4, 3, 4, 8, -1, -1, -1, -1 },
                new[] { 9, 5, 4, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 0, 11, 2, 0, 8, 11, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 0, 5, 4, 0, 1, 5, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 2, 1, 5, 2, 5, 8, 2, 8, 11, 4, 8, 5, -1, -1, -1, -1 },
                new[] { 10, 3, 11, 10, 1, 3, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 4, 9, 5, 0, 8, 1, 8, 10, 1, 8, 11, 10, -1, -1, -1, -1 },
                new[] { 5, 4, 0, 5, 0, 11, 5, 11, 10, 11, 0, 3, -1, -1, -1, -1 },
                new[] { 5, 4, 8, 5, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 9, 7, 8, 5, 7, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 9, 3, 0, 9, 5, 3, 5, 7, 3, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 0, 7, 8, 0, 1, 7, 1, 5, 7, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 1, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 9, 7, 8, 9, 5, 7, 10, 1, 2, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 10, 1, 2, 9, 5, 0, 5, 3, 0, 5, 7, 3, -1, -1, -1, -1 },
                new[] { 8, 0, 2, 8, 2, 5, 8, 5, 7, 10, 5, 2, -1, -1, -1, -1 },
                new[] { 2, 10, 5, 2, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 7, 9, 5, 7, 8, 9, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 9, 5, 7, 9, 7, 2, 9, 2, 0, 2, 7, 11, -1, -1, -1, -1 },
                new[] { 2, 3, 11, 0, 1, 8, 1, 7, 8, 1, 5, 7, -1, -1, -1, -1 },
                new[] { 11, 2, 1, 11, 1, 7, 7, 1, 5, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 9, 5, 8, 8, 5, 7, 10, 1, 3, 10, 3, 11, -1, -1, -1, -1 },
                new[] { 5, 7, 0, 5, 0, 9, 7, 11, 0, 1, 0, 10, 11, 10, 0, -1 },
                new[] { 11, 10, 0, 11, 0, 3, 10, 5, 0, 8, 0, 7, 5, 7, 0, -1 },
                new[] { 11, 10, 5, 7, 11, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 0, 8, 3, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 9, 0, 1, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 1, 8, 3, 1, 9, 8, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 1, 6, 5, 2, 6, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 1, 6, 5, 1, 2, 6, 3, 0, 8, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 9, 6, 5, 9, 0, 6, 0, 2, 6, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 5, 9, 8, 5, 8, 2, 5, 2, 6, 3, 2, 8, -1, -1, -1, -1 },
                new[] { 2, 3, 11, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 11, 0, 8, 11, 2, 0, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 0, 1, 9, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 5, 10, 6, 1, 9, 2, 9, 11, 2, 9, 8, 11, -1, -1, -1, -1 },
                new[] { 6, 3, 11, 6, 5, 3, 5, 1, 3, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 0, 8, 11, 0, 11, 5, 0, 5, 1, 5, 11, 6, -1, -1, -1, -1 },
                new[] { 3, 11, 6, 0, 3, 6, 0, 6, 5, 0, 5, 9, -1, -1, -1, -1 },
                new[] { 6, 5, 9, 6, 9, 11, 11, 9, 8, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 5, 10, 6, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 4, 3, 0, 4, 7, 3, 6, 5, 10, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 1, 9, 0, 5, 10, 6, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 10, 6, 5, 1, 9, 7, 1, 7, 3, 7, 9, 4, -1, -1, -1, -1 },
                new[] { 6, 1, 2, 6, 5, 1, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 1, 2, 5, 5, 2, 6, 3, 0, 4, 3, 4, 7, -1, -1, -1, -1 },
                new[] { 8, 4, 7, 9, 0, 5, 0, 6, 5, 0, 2, 6, -1, -1, -1, -1 },
                new[] { 7, 3, 9, 7, 9, 4, 3, 2, 9, 5, 9, 6, 2, 6, 9, -1 },
                new[] { 3, 11, 2, 7, 8, 4, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 5, 10, 6, 4, 7, 2, 4, 2, 0, 2, 7, 11, -1, -1, -1, -1 },
                new[] { 0, 1, 9, 4, 7, 8, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1 },
                new[] { 9, 2, 1, 9, 11, 2, 9, 4, 11, 7, 11, 4, 5, 10, 6, -1 },
                new[] { 8, 4, 7, 3, 11, 5, 3, 5, 1, 5, 11, 6, -1, -1, -1, -1 },
                new[] { 5, 1, 11, 5, 11, 6, 1, 0, 11, 7, 11, 4, 0, 4, 11, -1 },
                new[] { 0, 5, 9, 0, 6, 5, 0, 3, 6, 11, 6, 3, 8, 4, 7, -1 },
                new[] { 6, 5, 9, 6, 9, 11, 4, 7, 9, 7, 11, 9, -1, -1, -1, -1 },
                new[] { 10, 4, 9, 6, 4, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 4, 10, 6, 4, 9, 10, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 10, 0, 1, 10, 6, 0, 6, 4, 0, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 8, 3, 1, 8, 1, 6, 8, 6, 4, 6, 1, 10, -1, -1, -1, -1 },
                new[] { 1, 4, 9, 1, 2, 4, 2, 6, 4, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 3, 0, 8, 1, 2, 9, 2, 4, 9, 2, 6, 4, -1, -1, -1, -1 },
                new[] { 0, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 8, 3, 2, 8, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 10, 4, 9, 10, 6, 4, 11, 2, 3, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 0, 8, 2, 2, 8, 11, 4, 9, 10, 4, 10, 6, -1, -1, -1, -1 },
                new[] { 3, 11, 2, 0, 1, 6, 0, 6, 4, 6, 1, 10, -1, -1, -1, -1 },
                new[] { 6, 4, 1, 6, 1, 10, 4, 8, 1, 2, 1, 11, 8, 11, 1, -1 },
                new[] { 9, 6, 4, 9, 3, 6, 9, 1, 3, 11, 6, 3, -1, -1, -1, -1 },
                new[] { 8, 11, 1, 8, 1, 0, 11, 6, 1, 9, 1, 4, 6, 4, 1, -1 },
                new[] { 3, 11, 6, 3, 6, 0, 0, 6, 4, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 6, 4, 8, 11, 6, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 7, 10, 6, 7, 8, 10, 8, 9, 10, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 0, 7, 3, 0, 10, 7, 0, 9, 10, 6, 7, 10, -1, -1, -1, -1 },
                new[] { 10, 6, 7, 1, 10, 7, 1, 7, 8, 1, 8, 0, -1, -1, -1, -1 },
                new[] { 10, 6, 7, 10, 7, 1, 1, 7, 3, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 1, 2, 6, 1, 6, 8, 1, 8, 9, 8, 6, 7, -1, -1, -1, -1 },
                new[] { 2, 6, 9, 2, 9, 1, 6, 7, 9, 0, 9, 3, 7, 3, 9, -1 },
                new[] { 7, 8, 0, 7, 0, 6, 6, 0, 2, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 7, 3, 2, 6, 7, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 2, 3, 11, 10, 6, 8, 10, 8, 9, 8, 6, 7, -1, -1, -1, -1 },
                new[] { 2, 0, 7, 2, 7, 11, 0, 9, 7, 6, 7, 10, 9, 10, 7, -1 },
                new[] { 1, 8, 0, 1, 7, 8, 1, 10, 7, 6, 7, 10, 2, 3, 11, -1 },
                new[] { 11, 2, 1, 11, 1, 7, 10, 6, 1, 6, 7, 1, -1, -1, -1, -1 },
                new[] { 8, 9, 6, 8, 6, 7, 9, 1, 6, 11, 6, 3, 1, 3, 6, -1 },
                new[] { 0, 9, 1, 11, 6, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 7, 8, 0, 7, 0, 6, 3, 11, 0, 11, 6, 0, -1, -1, -1, -1 },
                new[] { 7, 11, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 3, 0, 8, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 0, 1, 9, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 8, 1, 9, 8, 3, 1, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 10, 1, 2, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 1, 2, 10, 3, 0, 8, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 2, 9, 0, 2, 10, 9, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 6, 11, 7, 2, 10, 3, 10, 8, 3, 10, 9, 8, -1, -1, -1, -1 },
                new[] { 7, 2, 3, 6, 2, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 7, 0, 8, 7, 6, 0, 6, 2, 0, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 2, 7, 6, 2, 3, 7, 0, 1, 9, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 1, 6, 2, 1, 8, 6, 1, 9, 8, 8, 7, 6, -1, -1, -1, -1 },
                new[] { 10, 7, 6, 10, 1, 7, 1, 3, 7, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 10, 7, 6, 1, 7, 10, 1, 8, 7, 1, 0, 8, -1, -1, -1, -1 },
                new[] { 0, 3, 7, 0, 7, 10, 0, 10, 9, 6, 10, 7, -1, -1, -1, -1 },
                new[] { 7, 6, 10, 7, 10, 8, 8, 10, 9, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 6, 8, 4, 11, 8, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 3, 6, 11, 3, 0, 6, 0, 4, 6, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 8, 6, 11, 8, 4, 6, 9, 0, 1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 9, 4, 6, 9, 6, 3, 9, 3, 1, 11, 3, 6, -1, -1, -1, -1 },
                new[] { 6, 8, 4, 6, 11, 8, 2, 10, 1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 1, 2, 10, 3, 0, 11, 0, 6, 11, 0, 4, 6, -1, -1, -1, -1 },
                new[] { 4, 11, 8, 4, 6, 11, 0, 2, 9, 2, 10, 9, -1, -1, -1, -1 },
                new[] { 10, 9, 3, 10, 3, 2, 9, 4, 3, 11, 3, 6, 4, 6, 3, -1 },
                new[] { 8, 2, 3, 8, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 0, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 1, 9, 0, 2, 3, 4, 2, 4, 6, 4, 3, 8, -1, -1, -1, -1 },
                new[] { 1, 9, 4, 1, 4, 2, 2, 4, 6, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 8, 1, 3, 8, 6, 1, 8, 4, 6, 6, 10, 1, -1, -1, -1, -1 },
                new[] { 10, 1, 0, 10, 0, 6, 6, 0, 4, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 4, 6, 3, 4, 3, 8, 6, 10, 3, 0, 3, 9, 10, 9, 3, -1 },
                new[] { 10, 9, 4, 6, 10, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 4, 9, 5, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 0, 8, 3, 4, 9, 5, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 5, 0, 1, 5, 4, 0, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 11, 7, 6, 8, 3, 4, 3, 5, 4, 3, 1, 5, -1, -1, -1, -1 },
                new[] { 9, 5, 4, 10, 1, 2, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 6, 11, 7, 1, 2, 10, 0, 8, 3, 4, 9, 5, -1, -1, -1, -1 },
                new[] { 7, 6, 11, 5, 4, 10, 4, 2, 10, 4, 0, 2, -1, -1, -1, -1 },
                new[] { 3, 4, 8, 3, 5, 4, 3, 2, 5, 10, 5, 2, 11, 7, 6, -1 },
                new[] { 7, 2, 3, 7, 6, 2, 5, 4, 9, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 9, 5, 4, 0, 8, 6, 0, 6, 2, 6, 8, 7, -1, -1, -1, -1 },
                new[] { 3, 6, 2, 3, 7, 6, 1, 5, 0, 5, 4, 0, -1, -1, -1, -1 },
                new[] { 6, 2, 8, 6, 8, 7, 2, 1, 8, 4, 8, 5, 1, 5, 8, -1 },
                new[] { 9, 5, 4, 10, 1, 6, 1, 7, 6, 1, 3, 7, -1, -1, -1, -1 },
                new[] { 1, 6, 10, 1, 7, 6, 1, 0, 7, 8, 7, 0, 9, 5, 4, -1 },
                new[] { 4, 0, 10, 4, 10, 5, 0, 3, 10, 6, 10, 7, 3, 7, 10, -1 },
                new[] { 7, 6, 10, 7, 10, 8, 5, 4, 10, 4, 8, 10, -1, -1, -1, -1 },
                new[] { 6, 9, 5, 6, 11, 9, 11, 8, 9, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 3, 6, 11, 0, 6, 3, 0, 5, 6, 0, 9, 5, -1, -1, -1, -1 },
                new[] { 0, 11, 8, 0, 5, 11, 0, 1, 5, 5, 6, 11, -1, -1, -1, -1 },
                new[] { 6, 11, 3, 6, 3, 5, 5, 3, 1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 1, 2, 10, 9, 5, 11, 9, 11, 8, 11, 5, 6, -1, -1, -1, -1 },
                new[] { 0, 11, 3, 0, 6, 11, 0, 9, 6, 5, 6, 9, 1, 2, 10, -1 },
                new[] { 11, 8, 5, 11, 5, 6, 8, 0, 5, 10, 5, 2, 0, 2, 5, -1 },
                new[] { 6, 11, 3, 6, 3, 5, 2, 10, 3, 10, 5, 3, -1, -1, -1, -1 },
                new[] { 5, 8, 9, 5, 2, 8, 5, 6, 2, 3, 8, 2, -1, -1, -1, -1 },
                new[] { 9, 5, 6, 9, 6, 0, 0, 6, 2, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 1, 5, 8, 1, 8, 0, 5, 6, 8, 3, 8, 2, 6, 2, 8, -1 },
                new[] { 1, 5, 6, 2, 1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 1, 3, 6, 1, 6, 10, 3, 8, 6, 5, 6, 9, 8, 9, 6, -1 },
                new[] { 10, 1, 0, 10, 0, 6, 9, 5, 0, 5, 6, 0, -1, -1, -1, -1 },
                new[] { 0, 3, 8, 5, 6, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 10, 5, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 11, 5, 10, 7, 5, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 11, 5, 10, 11, 7, 5, 8, 3, 0, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 5, 11, 7, 5, 10, 11, 1, 9, 0, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 10, 7, 5, 10, 11, 7, 9, 8, 1, 8, 3, 1, -1, -1, -1, -1 },
                new[] { 11, 1, 2, 11, 7, 1, 7, 5, 1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 0, 8, 3, 1, 2, 7, 1, 7, 5, 7, 2, 11, -1, -1, -1, -1 },
                new[] { 9, 7, 5, 9, 2, 7, 9, 0, 2, 2, 11, 7, -1, -1, -1, -1 },
                new[] { 7, 5, 2, 7, 2, 11, 5, 9, 2, 3, 2, 8, 9, 8, 2, -1 },
                new[] { 2, 5, 10, 2, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 8, 2, 0, 8, 5, 2, 8, 7, 5, 10, 2, 5, -1, -1, -1, -1 },
                new[] { 9, 0, 1, 5, 10, 3, 5, 3, 7, 3, 10, 2, -1, -1, -1, -1 },
                new[] { 9, 8, 2, 9, 2, 1, 8, 7, 2, 10, 2, 5, 7, 5, 2, -1 },
                new[] { 1, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 0, 8, 7, 0, 7, 1, 1, 7, 5, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 9, 0, 3, 9, 3, 5, 5, 3, 7, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 9, 8, 7, 5, 9, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 5, 8, 4, 5, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 5, 0, 4, 5, 11, 0, 5, 10, 11, 11, 3, 0, -1, -1, -1, -1 },
                new[] { 0, 1, 9, 8, 4, 10, 8, 10, 11, 10, 4, 5, -1, -1, -1, -1 },
                new[] { 10, 11, 4, 10, 4, 5, 11, 3, 4, 9, 4, 1, 3, 1, 4, -1 },
                new[] { 2, 5, 1, 2, 8, 5, 2, 11, 8, 4, 5, 8, -1, -1, -1, -1 },
                new[] { 0, 4, 11, 0, 11, 3, 4, 5, 11, 2, 11, 1, 5, 1, 11, -1 },
                new[] { 0, 2, 5, 0, 5, 9, 2, 11, 5, 4, 5, 8, 11, 8, 5, -1 },
                new[] { 9, 4, 5, 2, 11, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 2, 5, 10, 3, 5, 2, 3, 4, 5, 3, 8, 4, -1, -1, -1, -1 },
                new[] { 5, 10, 2, 5, 2, 4, 4, 2, 0, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 3, 10, 2, 3, 5, 10, 3, 8, 5, 4, 5, 8, 0, 1, 9, -1 },
                new[] { 5, 10, 2, 5, 2, 4, 1, 9, 2, 9, 4, 2, -1, -1, -1, -1 },
                new[] { 8, 4, 5, 8, 5, 3, 3, 5, 1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 0, 4, 5, 1, 0, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 8, 4, 5, 8, 5, 3, 9, 0, 5, 0, 3, 5, -1, -1, -1, -1 },
                new[] { 9, 4, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 4, 11, 7, 4, 9, 11, 9, 10, 11, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 0, 8, 3, 4, 9, 7, 9, 11, 7, 9, 10, 11, -1, -1, -1, -1 },
                new[] { 1, 10, 11, 1, 11, 4, 1, 4, 0, 7, 4, 11, -1, -1, -1, -1 },
                new[] { 3, 1, 4, 3, 4, 8, 1, 10, 4, 7, 4, 11, 10, 11, 4, -1 },
                new[] { 4, 11, 7, 9, 11, 4, 9, 2, 11, 9, 1, 2, -1, -1, -1, -1 },
                new[] { 9, 7, 4, 9, 11, 7, 9, 1, 11, 2, 11, 1, 0, 8, 3, -1 },
                new[] { 11, 7, 4, 11, 4, 2, 2, 4, 0, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 11, 7, 4, 11, 4, 2, 8, 3, 4, 3, 2, 4, -1, -1, -1, -1 },
                new[] { 2, 9, 10, 2, 7, 9, 2, 3, 7, 7, 4, 9, -1, -1, -1, -1 },
                new[] { 9, 10, 7, 9, 7, 4, 10, 2, 7, 8, 7, 0, 2, 0, 7, -1 },
                new[] { 3, 7, 10, 3, 10, 2, 7, 4, 10, 1, 10, 0, 4, 0, 10, -1 },
                new[] { 1, 10, 2, 8, 7, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 4, 9, 1, 4, 1, 7, 7, 1, 3, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 4, 9, 1, 4, 1, 7, 0, 8, 1, 8, 7, 1, -1, -1, -1, -1 },
                new[] { 4, 0, 3, 7, 4, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 4, 8, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 9, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 3, 0, 9, 3, 9, 11, 11, 9, 10, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 0, 1, 10, 0, 10, 8, 8, 10, 11, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 3, 1, 10, 11, 3, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 1, 2, 11, 1, 11, 9, 9, 11, 8, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 3, 0, 9, 3, 9, 11, 1, 2, 9, 2, 11, 9, -1, -1, -1, -1 },
                new[] { 0, 2, 11, 8, 0, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 3, 2, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 2, 3, 8, 2, 8, 10, 10, 8, 9, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 9, 10, 2, 0, 9, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 2, 3, 8, 2, 8, 10, 0, 1, 8, 1, 10, 8, -1, -1, -1, -1 },
                new[] { 1, 10, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 1, 3, 8, 9, 1, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 0, 9, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { 0, 3, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                new[] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }
                };
        }
    }
}
