using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Szeminarium1
{
    internal static class Program
    {
        private static IWindow graphicWindow;

        private static GL Gl;

        private static uint program;

        private static void CheckError(string lepes)
        { 
            GLEnum hiba;
            while ((hiba = Gl.GetError()) != GLEnum.NoError)
                Console.WriteLine($"GL hiba [{lepes}]: {hiba}");
        }

        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
		layout (location = 1) in vec4 vCol;

		out vec4 outCol;
        
        void main()
        {
			outCol = vCol;
            gl_Position = vec4(vPos.x, vPos.y, vPos.z, 1.0);
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

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "1. szeminárium - háromszög";
            windowOptions.Size = new Silk.NET.Maths.Vector2D<int>(500, 500);

            graphicWindow = Window.Create(windowOptions);

            graphicWindow.Load += GraphicWindow_Load;
            graphicWindow.Update += GraphicWindow_Update;
            graphicWindow.Render += GraphicWindow_Render;

            graphicWindow.Run();
        }

        private static void GraphicWindow_Load()
        {
            // egszeri beallitasokat
            //Console.WriteLine("Loaded");

            Gl = graphicWindow.CreateOpenGL();

            Gl.ClearColor(System.Drawing.Color.White);
            CheckError("Load: CreateOpenGL, ClearColor");

            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);
            CheckError("Load: CreateShader");

            Gl.ShaderSource(vshader, VertexShaderSource);
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));
            CheckError("Load: vertex shader fordítás");

            Gl.ShaderSource(fshader, FragmentShaderSource);
            Gl.CompileShader(fshader);
            // fragment shader forditasat is ellenorizzuk
            Gl.GetShader(fshader, ShaderParameterName.CompileStatus, out int fStatus);
            if (fStatus != (int)GLEnum.True)
                throw new Exception("Fragment shader failed to compile: " + Gl.GetShaderInfoLog(fshader));
            CheckError("Load: fragment shader fordítás");


            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            CheckError("Load: AttachShader");
            Gl.LinkProgram(program);
            CheckError("Load: LinkProgram");
            Gl.DetachShader(program, vshader);
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);
            CheckError("Load: Detach/DeleteShader");

            Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(program)}");
            }

        }

        private static void GraphicWindow_Update(double deltaTime)
        {
            // NO GL
            // make it threadsave
            //Console.WriteLine($"Update after {deltaTime} [s]");
        }

        private static unsafe void GraphicWindow_Render(double deltaTime)
        {
            //Console.WriteLine($"Render after {deltaTime} [s]");

            Gl.Clear(ClearBufferMask.ColorBufferBit);
            CheckError("Render: Clear");

            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);
            CheckError("Render: VAO létrehozás/bind");

            float[] vertexArray = new float[] {
                0.0f, 1.0f, 0.0f, // Teteje 0 - ez a felso laphoz
                -0.8f, 0.5f, 0.0f, // Bal felso 1 - ez a felso laphoz
                -0.8f, 0.5f, 0.0f, // Bal felso 2 - ez a bal laphoz
                0.0f, 0.0f, 0.0f, // Kozep 3 - ez a felso laphoz
                0.0f, 0.0f, 0.0f, // Kozep 4 - ez a bal laphoz
                0.0f, 0.0f, 0.0f, // Kozep 5 - ez a jobb laphoz
                0.8f, 0.5f, 0.0f, // Jobb felso 6 - ez a felso laphoz
                0.8f, 0.5f, 0.0f, // Jobb felso 7 - ez a jobb laphoz
                -0.8f, -0.5f, 0.0f, // Bal also 8 - ez a bal laphoz
                0.0f, -1.0f, 0.0f, // Also 9 - ez a bal laphoz
                0.0f, -1.0f, 0.0f, // Also 10 - ez a jobb laphoz
                0.8f, -0.5f, 0.0f, // Jobb also 11 - ez a jobb laphoz

                // Pontok a vonalakhoz, felosztva egy vonal 0, 1/3, 2/3, 1 reszekre
                // 1/3 pont: kezdoertek + (vegpont - kezdoertek) / 3
                // 2/3 pont: kezdoertek + (vegpont - kezdoertek) * 2 / 3
                // a Szemkozti oldalak 1/3 es 2/3 pontjai jonnek egymas utan parba

                // Felso lap - 4 belso vonal
                0.2667f,  0.8333f, 0f,
                -0.5333f,  0.3333f, 0f,
                0.5333f,  0.6667f, 0f,
                -0.2667f,  0.1667f, 0f,
                -0.2667f,  0.8333f, 0f,
                0.5333f,  0.3333f, 0f,
                -0.5333f,  0.6667f, 0f,
                0.2667f,  0.1667f, 0f,

                // Bal lap - 4 belso vonal
                -0.5333f,  0.3333f, 0f,
                -0.5333f, -0.6667f, 0f,
                -0.2667f,  0.1667f, 0f,
                -0.2667f, -0.8333f, 0f,
                -0.8f,     0.1667f, 0f,
                0f,      -0.3333f, 0f,
                -0.8f,    -0.1667f, 0f,
                0f,      -0.6667f, 0f,

                // Jobb lap - 4 belso vonal
                0.2667f,  0.1667f, 0f,
                0.2667f, -0.8333f, 0f,
                0.5333f,  0.3333f, 0f,
                0.5333f, -0.6667f, 0f,
                0f,      -0.3333f, 0f,
                0.8f,     0.1667f, 0f,
                0f,      -0.6667f, 0f,
                0.8f,    -0.1667f, 0f
            };

            float[] colorArray = new float[] {
                1.0f, 0.0f, 0.0f, 1.0f, // Teteje 0 - piros
                1.0f, 0.0f, 0.0f, 1.0f, // Bal felso 1 - piros
                0.0f, 1.0f, 0.0f, 1.0f, // Bal felso 2 - zold
                1.0f, 0.0f, 0.0f, 1.0f, // Kozep 3 - piros
                0.0f, 1.0f, 0.0f, 1.0f, // Kozep 4 - zold
                0.0f, 0.0f, 1.0f, 1.0f, // Kozep 5 - kek
                1.0f, 0.0f, 0.0f, 1.0f, // Jobb felso 6 - piros
                0.0f, 0.0f, 1.0f, 1.0f, // Jobb felso 7 - kek
                0.0f, 1.0f, 0.0f, 1.0f, // Bal also 8 - zold
                0.0f, 1.0f, 0.0f, 1.0f, // Also 9 - zold
                0.0f, 0.0f, 1.0f, 1.0f, // Also 10 - kek
                0.0f, 0.0f, 1.0f, 1.0f, // Jobb also 11 - kek
            };

            uint[] indexArray = new uint[] { 
                0, 1, 3, // Teteje, Bal felso, Kozep -> Felso lap bal oldala 
                0, 3, 6, // Teteje, Kozep, Jobb felso -> Felso lap jobb oldala
                2, 8, 9, // Bal felso, Bal also, Also -> Bal lap also oldala
                2, 9, 4, // Bal felso, Also, Kozep -> Bal lap felso oldala
                5, 10, 7, // Kozep, Also, Jobb felso -> Jobb lap felso oldala
                7, 10, 11, // Jobb felso, Also, Jobb also -> Jobb lap also oldala
            };

            uint vertices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)vertexArray.AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, null); // Invalid Value az 5-os
            Gl.EnableVertexAttribArray(0);
            CheckError("Render: vertices buffer + attribútum 0");

            uint colors = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, colors);

            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)colorArray.AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(1);
            CheckError("Render: colors buffer + attribútum 1");

            uint indices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
            Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)indexArray.AsSpan(), GLEnum.StaticDraw);
            CheckError("Render: indices buffer");

            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);

            Gl.UseProgram(program);
            CheckError("Render: UseProgram");

            Gl.DrawElements(GLEnum.Triangles, (uint)indexArray.Length, GLEnum.UnsignedInt, null); // we used element buffer
            CheckError("Render: DrawElements");
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);
            Gl.BindVertexArray(vao);

            // szin attributumot kikapcsoljuk, hogy legyenek feketek a vonalak
            Gl.DisableVertexAttribArray(1);
            // A 12-es indextol 24 pontot rajzolunk ki
            // 24 pont = 12 vonal, mert minden vonalhoz 2 vegpont kell
            Gl.DrawArrays(GLEnum.Lines, 12, 24);

            // always unbound the vertex buffer first, so no halfway results are displayed by accident
            Gl.DeleteBuffer(vertices);
            Gl.DeleteBuffer(colors);
            Gl.DeleteBuffer(indices);
            Gl.DeleteVertexArray(vao);
            CheckError("Render: takarítás");
        }
    }
}
