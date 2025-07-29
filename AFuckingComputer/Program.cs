using System.Drawing.Drawing2D;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.InteropServices;

namespace Computer;

public static class Program
{
    [DllImport("kernel32.dll")]
    static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    static extern bool FreeConsole();


    [STAThread]
    static void Main()
    {
        AllocConsole();

        Application.EnableVisualStyles();

        var computer = new Computer();
        var window = new PixelWindow(computer.Gpu, computer.Input);

        var compiler = new CSharpCompiler();

        string csharpCode = @"
static void Main()
{
    
    byte x = 10;
    byte y = 10;
    byte dx = 1;
    byte dy = 1;

    byte px = x;
    byte py = y;

    while (true)
    {
        SetPixel(px, py, 0, 0, 0);

        px = x;
        py = y;

        x = x + dx;
        y = y + dy;

        if (x == 0)
        {
            dx = 0 - dx;
        }
        if (x == 63)
        {
            dx = 0 - dx;
        }
        
        if (y == 0)
        {
            dy = 0 - dy;
        }
        if (y == 63)
        {
            dy = 0 - dy;
        }
        SetPixel(x, y, 255, 0, 0);
    }
    
}";

        string drawLine = @"
static void Main()
{
    
    byte x = 64;
    while (x < 255)
    {
        while (x > 0)
        {
            x = x - 1;
            SetPixel(x, x, 255, 0, 0);
        }
        while (x < 64)
        {
            x = x + 1;
            SetPixel(x, x, 0, 255, 0);
            
        }
    }
}";

        try
        {
            byte[] bytecode = compiler.Compile(drawLine);

            Console.WriteLine("Compiled bytecode:");
            Console.WriteLine(compiler.DisassembleBytecode(bytecode));

            var constants = compiler.GetConstants();
            foreach (var kvp in constants)
            {
                computer.Ram.ram[kvp.Value] = kvp.Key;
            }

            byte[] programOnly = compiler.GetProgramBytecode();
            computer.LoadProgram(programOnly);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Comp error: {ex.Message}");
        }


        var computerThread = new Thread(() => computer.Run());
        computerThread.IsBackground = true;
        computerThread.Start();
        
        Application.Run(window);
    }

    // Drawing three colored pixels to screen
    public static byte[] CreateDrawingProgram()
    {
        return Assembler.Assemble(
            "SET_X", "10",         // SET_X 10
            "SET_Y", "10",         // SET_Y 10  
            "SET_R", "255",        // SET_R 255 (red)
            "SET_G", "0",          // SET_G 0
            "SET_B", "0",          // SET_B 0
            "DRAW_PIXEL", "0",     // DRAW_PIXEL

            "SET_X", "20",         // SET_X 20
            "SET_Y", "20",         // SET_Y 20
            "SET_R", "0",          // SET_R 0
            "SET_G", "255",        // SET_G 255 (green)
            "SET_B", "0",          // SET_B 0
            "DRAW_PIXEL", "0",     // DRAW_PIXEL

            "SET_X", "30",         // SET_X 30
            "SET_Y", "30",         // SET_Y 30
            "SET_R", "0",          // SET_R 0
            "SET_G", "0",          // SET_G 0
            "SET_B", "255",        // SET_B 255 (blue)
            "DRAW_PIXEL", "0",     // DRAW_PIXEL

            "HLT", "0"             // HLT
        );
    }

    // Add two memory values together
    public static byte[] CreateAdditionProgram()
    {
        return Assembler.Assemble(
            "LOAD", "0x10",        // LOAD 0x10 ;; loads 5 into ACC
            "ADD", "0x11",         // ADD  0x11 ;; adds 3 to loaded 5
            "STORE", "0x12",       // STORE 0x12 ;; stores result in 0x12
            "HLT", "0"             // HLT ;; stops
        );
        // computer.Ram.ram[0x10] = 5; computer.Ram.ram[0x11] = 3;
    }

    // Draw diagonal line using loop
    public static byte[] CreateLineDrawingProgram()
    {
        return Assembler.Assemble(
            // loop_start: (address 0x00)
            "LOAD", "0xF0",        // LOAD 0xF0 => ACC = loop counter
            "SET_X_FROM_ACC", "0", // SET_X_FROM_ACC  
            "SET_Y_FROM_ACC", "0", // SET_Y_FROM_ACC

            "SET_R", "255",        // SET_R 255
            "SET_G", "0",          // SET_G 0
            "SET_B", "0",          // SET_B 0
            "DRAW_PIXEL", "0",     // DRAW_PIXEL

            // Inc counter
            "LOAD", "0xF0",        // LOAD loop counter again
            "ADD", "0xF1",         // ADD 0xF1 => ACC += 1  
            "STORE", "0xF0",       // STORE back to 0xF0

            // Check if continue looping
            "LOAD", "0xF0",        // LOAD loop counter again (important!)
            "CMP", "0xF2",         // CMP 0xF2 (limit)
            "JNZ", "0x00",         // JNZ 0x00 (jump back to loop_start if not equal)

            "HLT", "0"             // HLT
        );
        // computer.Ram.ram[0xF0] = 0; computer.Ram.ram[0xF1] = 1; computer.Ram.ram[0xF2] = 50;
    }

    public static byte[] FlappybirdClone()
    {
        return Assembler.Assemble(
            // init:
            "LOAD", "0x10",             // center_x
            "STORE", "0xF0",            // x_pos
            "LOAD", "0x11",             // start_y
            "STORE", "0xF1",            // y_pos

            // loop_start (address 6):
            // clear old pixel
            "LOAD", "0xF0",             // x_pos
            "SET_X_FROM_ACC", "0",
            "LOAD", "0xF1",
            "SET_Y_FROM_ACC", "0",
            "SET_R", "0",
            "SET_G", "0",
            "SET_B", "0",
            "DRAW_PIXEL", "0",

            // gravity
            "LOAD", "0xF1",             // y_pos
            "ADD", "0x12",              // +1
            "STORE", "0xF1",

            // check input
            "KEY_AVAILABLE", "0",
            "JZ", "36",                 // if no key, skip input

            "GET_KEY", "0",
            "CMP_VAL", "32",            // spacebar?
            "JNZ", "36",

            // jump up
            "LOAD", "0xF1",
            "SUB", "0x12",              // -1
            "SUB", "0x12",              // -1
            "SUB", "0x12",              // -1
            "STORE", "0xF1",

            // draw new pixel (at new y)
            "LOAD", "0xF0",
            "SET_X_FROM_ACC", "0",
            "LOAD", "0xF1",
            "SET_Y_FROM_ACC", "0",
            "SET_R", "255",
            "SET_G", "255",
            "SET_B", "255",
            "DRAW_PIXEL", "0",

            // loop
            "JMP", "6",
            "HLT", "0"
        );
    }

}

public class Computer
{
    public RAM Ram { get; }
    public CPU Cpu { get; }
    public GPU Gpu { get; }
    public Input Input { get; }

    public Computer()
    {
        Ram = new RAM(4096); // 4KB
        Gpu = new GPU();
        Input = new Input();
        Cpu = new CPU(Ram, Gpu, Input);
    }

    public void LoadProgram(byte[] program)
    {
        Array.Copy(program, Ram.ram, program.Length);
    }

    public void Run()
    {
        Cpu.Run();
    }
}

