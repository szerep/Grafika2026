using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Szeminarium;

namespace GrafikaSzeminarium
{
    internal class Program
    {
        private static IWindow graphicWindow;

        private static GL Gl;

        //3x3x3-as tömb, a kis kocka helye az index (x+1, y+1, z+1)
        private static ModelObjectDescriptor[,,] cubes = new ModelObjectDescriptor[3, 3, 3];

        private static CameraDescriptor camera = new CameraDescriptor();

        private static CubeArrangementModel cubeArrangementModel = new CubeArrangementModel();

        private const string ModelMatrixVariableName = "uModel";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";

        // kamera hol all, es merre nez
        private static Vector3D<float> cameraPosition = new Vector3D<float>(4f, 3f, 6f);
        private static float cameraYaw = -0.59f;
        private static float cameraPitch = -0.39f;
        private const float MoveStep = 0.3f;
        private const float TurnStep = 0.05f;

        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
		layout (location = 1) in vec4 vCol;

        uniform mat4 uModel;
        uniform mat4 uView;
        uniform mat4 uProjection;

		out vec4 outCol;
        
        void main()
        {
			outCol = vCol;
            gl_Position = uProjection*uView*uModel*vec4(vPos.x, vPos.y, vPos.z, 1.0);
        }
        ";


        private static readonly string FragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;
		
		in vec4 outCol;

        void main()
        {
            FragColor = outCol;
        }
        ";

        private static uint program;

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "Grafika szeminárium";
            windowOptions.Size = new Silk.NET.Maths.Vector2D<int>(500, 500);

            graphicWindow = Window.Create(windowOptions);

            graphicWindow.Load += GraphicWindow_Load;
            graphicWindow.Update += GraphicWindow_Update;
            graphicWindow.Render += GraphicWindow_Render;
            graphicWindow.Closing += GraphicWindow_Closing;

            graphicWindow.Run();
        }

        private static void GraphicWindow_Closing()
        {
            foreach (var c in cubes)
                c.Dispose();
            Gl.DeleteProgram(program);
        }

        private static void GraphicWindow_Load()
        {
            Gl = graphicWindow.CreateOpenGL();

            var inputContext = graphicWindow.CreateInput();
            foreach (var keyboard in inputContext.Keyboards)
            {
                keyboard.KeyDown += Keyboard_KeyDown;
            }

            // 27 kis kocka, mind kulon VAO-val es sajat szinnel
            // x, y, z a kocka helye a racson: -1, 0, 1
            for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                    for (int z = -1; z <= 1; z++)
                        cubes[x + 1, y + 1, z + 1] = ModelObjectDescriptor.CreateCube(Gl, ModelObjectDescriptor.BuildColorArray(x, y, z));

            Gl.ClearColor(System.Drawing.Color.White);
            
            Gl.Enable(EnableCap.CullFace);
            Gl.CullFace(TriangleFace.Back);

            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);


            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, VertexShaderSource);
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

            Gl.ShaderSource(fshader, FragmentShaderSource);
            Gl.CompileShader(fshader);
            Gl.GetShader(fshader, ShaderParameterName.CompileStatus, out int fStatus);
            if (fStatus != (int)GLEnum.True)
                throw new Exception("Fragment shader failed to compile: " + Gl.GetShaderInfoLog(fshader));

            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);

            Gl.DetachShader(program, vshader);
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);
            if ((ErrorCode)Gl.GetError() != ErrorCode.NoError)
            {

            }

            Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(program)}");
            }

            // uniformot csak aktiv programnal lehet beallitani
            Gl.UseProgram(program);

            // az ablak 500x500 -> kepearany 1
            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)(Math.PI / 4), 1f, 0.1f, 100f);
            SetMatrix(projectionMatrix, ProjectionMatrixVariableName);
        }

        private static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            var forward = GetForward();
            // jobbra mutato irany: vizszintes, meroleges az elore irannyal
            var right = new Vector3D<float>(MathF.Cos(cameraYaw), 0f, MathF.Sin(cameraYaw));
            switch (key)
            {
                // mozgás
                case Key.W: cameraPosition += forward * MoveStep; break;   // előre
                case Key.S: cameraPosition -= forward * MoveStep; break;   // hátra
                case Key.D: cameraPosition += right * MoveStep; break;     // jobbra
                case Key.A: cameraPosition -= right * MoveStep; break;     // balra
                case Key.Q: cameraPosition.Y += MoveStep; break;           // fel
                case Key.Z: cameraPosition.Y -= MoveStep; break;           // le

                // nézési irány: nyilak
                case Key.Left: cameraYaw -= TurnStep; break;     // balra fordul
                case Key.Right: cameraYaw += TurnStep; break;     // jobbra fordul
                case Key.Up: cameraPitch += TurnStep; break;   // felfelé néz
                case Key.Down: cameraPitch -= TurnStep; break;   // lefelé néz

                case Key.Space:
                    cubeArrangementModel.AnimationEnabled = !cubeArrangementModel.AnimationEnabled;
                    break;
            }

            cameraPitch = Math.Clamp(cameraPitch, -1.5f, 1.5f);
        }

        private static void GraphicWindow_Update(double deltaTime)
        {
            // NO OpenGL
            // make it threadsafe
            cubeArrangementModel.AdvanceTime(deltaTime);
        }

        private static unsafe void GraphicWindow_Render(double deltaTime)
        {
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);

            Gl.UseProgram(program);

            // a kamera poz es iranyabol mindig  ujraszamoljuk a nezetet
            var viewMatrix = Matrix4X4.CreateLookAt(cameraPosition, cameraPosition + GetForward(), Vector3D<float>.UnitY);
            SetMatrix(viewMatrix, ViewMatrixVariableName);

            for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                    for (int z = -1; z <= 1; z++)
                    {
                        // 0.9-re kicsinyitjuk, igy 0.1 res marad a kockak kozott, aztan a helyere toljuk
                        var modelMatrix = Matrix4X4.CreateScale(0.9f) * Matrix4X4.CreateTranslation((float)x, (float)y, (float)z);
                        SetMatrix(modelMatrix, ModelMatrixVariableName);
                        DrawModelObject(cubes[x + 1, y + 1, z + 1]);
                    }
        }

        // Megmondja, merre nez a kamera
        private static Vector3D<float> GetForward()
        {
            return new Vector3D<float>(
                MathF.Sin(cameraYaw) * MathF.Cos(cameraPitch),
                MathF.Sin(cameraPitch),
                -MathF.Cos(cameraYaw) * MathF.Cos(cameraPitch));
        }

        private static unsafe void DrawModelObject(ModelObjectDescriptor modelObject)
        {
            Gl.BindVertexArray(modelObject.Vao);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, modelObject.Indices);
            Gl.DrawElements(PrimitiveType.Triangles, modelObject.IndexArrayLength, DrawElementsType.UnsignedInt, null);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);
            Gl.BindVertexArray(0);
        }

        private static unsafe void SetMatrix(Matrix4X4<float> mx, string uniformName)
        {
            int location = Gl.GetUniformLocation(program, uniformName);
            if (location == -1)
            {
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&mx);
            CheckError();
        }

        public static void CheckError()
        {
            var error = (ErrorCode)Gl.GetError();
            if (error != ErrorCode.NoError)
                throw new Exception("GL.GetError() returned " + error.ToString());
        }
    }
}