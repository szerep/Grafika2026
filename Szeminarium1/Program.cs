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

        // 'verison 330 core' torlese -> forditasi hiba
        // ha cserelem a location szamokat, akkor a szinek/alakzatok elcsusznak
        // outCol valtozo atnevezese -> linkelesi hiba
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
            Gl.CompileShader(vshader); // vertex shader failed to compile
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));
            CheckError("Load: vertex shader fordítás");

            // HA felcserelem a CompileShader es ShaderSource sorrendet, akkor a shader forditas sikertelen lesz -> nem rajzol ki semmit a program
            Gl.ShaderSource(fshader, FragmentShaderSource);
            // Ha kihagyom a ShaderSource hivasat akkor semmit se rajzol ki
            Gl.CompileShader(fshader);
            // fragment shader forditasat is ellenorizzuk
            Gl.GetShader(fshader, ShaderParameterName.CompileStatus, out int fStatus);
            if (fStatus != (int)GLEnum.True)
                throw new Exception("Fragment shader failed to compile: " + Gl.GetShaderInfoLog(fshader));
            CheckError("Load: fragment shader fordítás");


            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            // AttachShaer(fshader) torlese -> fekete lesz a kep, de a shader forditas sikeres lesz
            Gl.AttachShader(program, fshader); // GL hiba [Load: Detach/DeleteShader]: InvalidOperation
            CheckError("Load: AttachShader");
            // Ugyan ugy fekete ha a LinkProgram-ot az AttachShader(fshader) elott hivom meg
            // Ha a DetachShader es DeleteShader a LinkProgram ele kerul, a rajz fekete lesz
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

            //vao vagy a bindeles torlese -> nem lesz rajz
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);
            CheckError("Render: VAO létrehozás/bind");

            // szam torlese / modositasa -> az alakzat elcsuszik
            float[] vertexArray = new float[] {
                -0.5f, -0.5f, 0.0f,
                +0.5f, -0.5f, 0.0f,
                 0.0f, +0.5f, 0.0f,
                 1f, 1f, 0f
            };

            // szam torlese / modositasa -> a szinek elcsusznak
            float[] colorArray = new float[] {
                1.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
            };

            // Ha torlok egy indexet akkor csak egyik haromszog jelenik meg
            uint[] indexArray = new uint[] { 
                0, 1, 2,
                2, 1, 3
            };

            uint vertices = Gl.GenBuffer();
            // Bindbuffer torlese -> nem lesz rajz, nem indul a program
            Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)vertexArray.AsSpan(), GLEnum.StaticDraw);
            // meret 3- > 5 nem fut le
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, null); // Invalid Value az 5-os
            Gl.EnableVertexAttribArray(0); // ha ez 1 akkor nem rajzolodik ki semmi
            CheckError("Render: vertices buffer + attribútum 0");

            uint colors = Gl.GenBuffer();
            // Bindbuffer torlese -> nem lesz rajz, nem indul a program
            Gl.BindBuffer(GLEnum.ArrayBuffer, colors);

            // BufferData torlese -> nem lesz rajz, nem indul a program
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)colorArray.AsSpan(), GLEnum.StaticDraw);
            // meret 4- > 3 a szinek elcsusznak, 4- > 5 nem fut le
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            // torles fekete haromszogeket ad
            // parameterv altoztatasa fekete lesz a rajz
            Gl.EnableVertexAttribArray(1);
            CheckError("Render: colors buffer + attribútum 1");

            uint indices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
            Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)indexArray.AsSpan(), GLEnum.StaticDraw);
            CheckError("Render: indices buffer");

            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);

            // UserProgram torlese -> fekete haromszogeket ad
            Gl.UseProgram(program);
            CheckError("Render: UseProgram");

            Gl.DrawElements(GLEnum.Triangles, (uint)indexArray.Length, GLEnum.UnsignedInt, null); // we used element buffer
            CheckError("Render: DrawElements");
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);
            Gl.BindVertexArray(vao);

            // always unbound the vertex buffer first, so no halfway results are displayed by accident
            Gl.DeleteBuffer(vertices);
            Gl.DeleteBuffer(colors);
            Gl.DeleteBuffer(indices);
            Gl.DeleteVertexArray(vao);
            CheckError("Render: takarítás");
        }
    }
}
