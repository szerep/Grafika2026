using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Numerics;
using Szeminarium;
using Silk.NET.OpenGL.Extensions.ImGui;

namespace GrafikaSzeminarium
{
    internal class Program
    {
        private static IWindow graphicWindow;

        private static GL Gl;

        private static ImGuiController imGuiController;

        //3x3x3-as tömb, a kis kocka helye az index (x+1, y+1, z+1)
        private static ModelObjectDescriptor[,,] cubes = new ModelObjectDescriptor[3, 3, 3];

        private static CameraDescriptor camera = new CameraDescriptor();

        private static CubeArrangementModel cubeArrangementModel = new CubeArrangementModel();

        private const string ModelMatrixVariableName = "uModel";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";
        private const string SliceRotationVariableName = "uSliceRotation";

        private const string NormalMatrixVariableName = "uNormal";
        private const string LightColorVariableName = "uLightColor";
        private const string LightPositionVariableName = "uLightPos";
        private const string ViewPositionVariableName = "uViewPos";
        private const string ShinenessVariableName = "uShininess";

        private static float shininess = 50;

        // a fenyforras szine es pozicioja
        private static Vector3 lightColor = new Vector3(1f, 1f, 1f);
        private static Vector3 lightPosition = new Vector3(3f, 5f, 5f);

        // kamera hol all, es merre nez
        private static Vector3D<float> cameraPosition = new Vector3D<float>(4f, 3f, 6f);
        private static float cameraYaw = -0.59f;
        private static float cameraPitch = -0.39f;
        private const float MoveStep = 0.3f;
        private const float TurnStep = 0.05f;

        // a felso reteg forgtasa: az akt es a cel szog
        private static float sliceAngle = 0f;
        private static float sliceTargetAngle = 0f;

        // forgas sebessege: 90 fok (PI/2) / mp
        private const float SliceSpeed = MathF.PI / 2;

        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
		layout (location = 1) in vec4 vCol;
        layout (location = 2) in vec3 vNormal;

        uniform mat4 uModel;
        uniform mat3 uNormal;
        uniform mat4 uView;
        uniform mat4 uProjection;
        uniform mat4 uSliceRotation;   // szelet forgatasa

		out vec4 outCol;
        out vec3 outNormal;
        out vec3 outWorldPosition;
        
        void main()
        {
            outCol = vCol;
            // a normalist es a poziciot is ugyanugy mozgatjuk: eloszor a modell, utana a szelet forgatasa
            outNormal = mat3(uSliceRotation) * (uNormal * vNormal);
            outWorldPosition = vec3(uSliceRotation * uModel * vec4(vPos, 1.0));
            gl_Position = uProjection * uView * uSliceRotation * uModel * vec4(vPos, 1.0);
        }
        ";


        private static readonly string FragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;

        uniform vec3 uLightColor;
        uniform vec3 uLightPos;
        uniform vec3 uViewPos;

        uniform float uShininess;

        in vec4 outCol;
        in vec3 outNormal;
        in vec3 outWorldPosition;

        void main()
        {
            float ambientStrength = 0.2;
            vec3 ambient = ambientStrength * uLightColor;

            float diffuseStrength = 0.8;
            vec3 norm = normalize(outNormal);
            vec3 lightDir = normalize(uLightPos - outWorldPosition);
            float diff = max(dot(norm, lightDir), 0.0);
            vec3 diffuse = diff * uLightColor * diffuseStrength;

            vec3 viewDir = normalize(uViewPos - outWorldPosition);
            vec3 reflectDir = reflect(-lightDir, norm);
            float spec = pow(max(dot(viewDir, reflectDir), 0.0), uShininess);

            vec3 result = (ambient + diffuse + spec) * outCol.rgb;

            FragColor = vec4(result, outCol.w);
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

            imGuiController = new ImGuiController(Gl, graphicWindow, inputContext);

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
            if (ImGuiNET.ImGui.GetIO().WantTextInput)
                return;
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
                    sliceTargetAngle += MathF.PI / 2;   // +90 fok az egyik iranyba
                    break;
                case Key.Backspace:
                    sliceTargetAngle -= MathF.PI / 2;   // -90 fok a masik iranyba
                    break;
            }

            cameraPitch = Math.Clamp(cameraPitch, -1.5f, 1.5f);
        }

        private static void GraphicWindow_Update(double deltaTime)
        {
            // NO OpenGL
            // make it threadsafe
            cubeArrangementModel.AdvanceTime(deltaTime);
            imGuiController.Update((float)deltaTime);
            // egy kepkockanal ennyit fordulhat
            float step = SliceSpeed * (float)deltaTime;

            if (sliceAngle < sliceTargetAngle)
                sliceAngle = MathF.Min(sliceAngle + step, sliceTargetAngle);   // nem lovi tul a celt
            else if (sliceAngle > sliceTargetAngle)
                sliceAngle = MathF.Max(sliceAngle - step, sliceTargetAngle);
        }

        private static unsafe void GraphicWindow_Render(double deltaTime)
        {
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);

            Gl.UseProgram(program);

            SetUniform3(LightColorVariableName, lightColor);
            SetUniform3(LightPositionVariableName, lightPosition);
            SetUniform3(ViewPositionVariableName, new Vector3(cameraPosition.X, cameraPosition.Y, cameraPosition.Z));
            SetUniform1(ShinenessVariableName, shininess);

            // a kamera poz es iranyabol mindig  ujraszamoljuk a nezetet
            var viewMatrix = Matrix4X4.CreateLookAt(cameraPosition, cameraPosition + GetForward(), Vector3D<float>.UnitY);
            SetMatrix(viewMatrix, ViewMatrixVariableName);

            for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                    for (int z = -1; z <= 1; z++)
                    {
                        // 0.9-re kicsinyitjuk, igy 0.1 res marad a kockak kozott, aztan a helyere toljuk
                        var modelMatrix = Matrix4X4.CreateScale(0.9f) * Matrix4X4.CreateTranslation((float)x, (float)y, (float)z);
                        SetModelMatrix(modelMatrix);
                        // a felso reteg (y == 1) kapja a forgatast, a tobbi egysegmatrixot
                        Matrix4X4<float> sliceRotation = Matrix4X4<float>.Identity;
                        if (y == 1)
                            sliceRotation = Matrix4X4.CreateRotationY(sliceAngle);
                        SetMatrix(sliceRotation, SliceRotationVariableName);
                        DrawModelObject(cubes[x + 1, y + 1, z + 1]);
                    }

            ImGuiNET.ImGui.Begin("Feny", ImGuiNET.ImGuiWindowFlags.AlwaysAutoResize | ImGuiNET.ImGuiWindowFlags.NoCollapse);

            // a megvilagitas szinosszetetele: 3 csuszka
            ImGuiNET.ImGui.SliderFloat("Piros", ref lightColor.X, 0f, 1f);
            ImGuiNET.ImGui.SliderFloat("Zold", ref lightColor.Y, 0f, 1f);
            ImGuiNET.ImGui.SliderFloat("Kek", ref lightColor.Z, 0f, 1f);

            // a fenyforras helye: 3 szoveges mezo
            ImGuiNET.ImGui.InputFloat("Feny X", ref lightPosition.X);
            ImGuiNET.ImGui.InputFloat("Feny Y", ref lightPosition.Y);
            ImGuiNET.ImGui.InputFloat("Feny Z", ref lightPosition.Z);

            // a szelet forgatasa gombokkal
            if (ImGuiNET.ImGui.Button("Forgatas +90"))
                sliceTargetAngle += MathF.PI / 2;
            ImGuiNET.ImGui.SameLine();
            if (ImGuiNET.ImGui.Button("Forgatas -90"))
                sliceTargetAngle -= MathF.PI / 2;

            ImGuiNET.ImGui.End();

            imGuiController.Render();
        }

        // Megmondja, merre nez a kamera
        private static Vector3D<float> GetForward()
        {
            return new Vector3D<float>(
                MathF.Sin(cameraYaw) * MathF.Cos(cameraPitch),
                MathF.Sin(cameraPitch),
                -MathF.Cos(cameraYaw) * MathF.Cos(cameraPitch));
        }

        private static unsafe void SetModelMatrix(Matrix4X4<float> modelMatrix)
        {
            SetMatrix(modelMatrix, ModelMatrixVariableName);

            // a normalmatrixot is beallitjuk
            int location = Gl.GetUniformLocation(program, NormalMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{NormalMatrixVariableName} uniform not found on shader.");
            }

            // G = (M^-1)^T
            var modelMatrixWithoutTranslation = new Matrix4X4<float>(modelMatrix.Row1, modelMatrix.Row2, modelMatrix.Row3, modelMatrix.Row4);
            modelMatrixWithoutTranslation.M41 = 0;
            modelMatrixWithoutTranslation.M42 = 0;
            modelMatrixWithoutTranslation.M43 = 0;
            modelMatrixWithoutTranslation.M44 = 1;

            Matrix4X4<float> modelInvers;
            Matrix4X4.Invert<float>(modelMatrixWithoutTranslation, out modelInvers);
            Matrix3X3<float> normalMatrix = new Matrix3X3<float>(Matrix4X4.Transpose(modelInvers));

            Gl.UniformMatrix3(location, 1, false, (float*)&normalMatrix);
            CheckError();
        }

        private static unsafe void SetUniform1(string uniformName, float uniformValue)
        {
            int location = Gl.GetUniformLocation(program, uniformName);
            if (location == -1)
            {
                throw new Exception($"{uniformName} uniform not found on shader.");
            }

            Gl.Uniform1(location, uniformValue);
            CheckError();
        }

        private static unsafe void SetUniform3(string uniformName, Vector3 uniformValue)
        {
            int location = Gl.GetUniformLocation(program, uniformName);
            if (location == -1)
            {
                throw new Exception($"{uniformName} uniform not found on shader.");
            }

            Gl.Uniform3(location, uniformValue);
            CheckError();
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