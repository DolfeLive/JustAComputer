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

        Console.WriteLine("test");
        Application.EnableVisualStyles();

        var computer = new Computer();
        var window = new PixelWindow(computer.Gpu, computer.Input);

        computer.LoadProgram(FlappybirdClone());
        computer.Ram.ram[0x10] = 32; // center x
        computer.Ram.ram[0x11] = 32;  // start y
        computer.Ram.ram[0x12] = 1;  // +1 (used for gravity and jump)

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

    // WIP not working, idk
    public static byte[] CreateArrowKeyProgram()
    {
        return Assembler.Assemble(
            "LOAD", "0xF5",          // LOAD center_x (32)
            "STORE", "0xF2",         // STORE current_x
            "LOAD", "0xF5",          // LOAD center_y (32) 
            "STORE", "0xF3",         // STORE current_y

            // main_loop: (starts at address 8)
            "KEY_AVAILABLE", "0",    // KEY_AVAILABLE
            "JZ", "8",               // JZ main_loop => wait for key (jump to address 8)

            "GET_KEY", "0",          // GET_KEY
            "STORE", "0xF0",         // STORE current_key

            // Clear old pixel first
            "LOAD", "0xF2",          // LOAD current_x
            "SET_X_FROM_ACC", "0",   // SET_X_FROM_ACC
            "LOAD", "0xF3",          // LOAD current_y
            "SET_Y_FROM_ACC", "0",   // SET_Y_FROM_ACC
            "SET_R", "0",            // SET_R 0 (black to clear)
            "SET_G", "0",            // SET_G 0
            "SET_B", "0",            // SET_B 0
            "DRAW_PIXEL", "0",       // DRAW_PIXEL (clear old position)

            // Check Up arrow (0x80) - address ~32
            "LOAD", "0xF0",          // LOAD current_key
            "CMP_VAL", "128",        // CMP_VAL 128 (0x80 = up arrow)
            "JNZ", "42",             // JNZ check_down (jump to address 42)
            "LOAD", "0xF3",          // LOAD current_y
            "SUB", "0xF8",           // SUB one
            "STORE", "0xF3",         // STORE current_y
            "JMP", "70",             // JMP draw (jump to address 70)

            // check_down: (address ~42)
            "LOAD", "0xF0",          // LOAD current_key
            "CMP_VAL", "129",        // CMP_VAL 129 (0x81 = down arrow)
            "JNZ", "52",             // JNZ check_left (jump to address 52)
            "LOAD", "0xF3",          // LOAD current_y
            "ADD", "0xF8",           // ADD one
            "STORE", "0xF3",         // STORE current_y
            "JMP", "70",             // JMP draw (jump to address 70)

            // check_left: (address ~52)
            "LOAD", "0xF0",          // LOAD current_key
            "CMP_VAL", "130",        // CMP_VAL 130 (0x82 = left arrow)
            "JNZ", "62",             // JNZ check_right (jump to address 62)
            "LOAD", "0xF2",          // LOAD current_x
            "SUB", "0xF8",           // SUB one
            "STORE", "0xF2",         // STORE current_x
            "JMP", "70",             // JMP draw (jump to address 70)

            // check_right: (address ~62)
            "LOAD", "0xF0",          // LOAD current_key
            "CMP_VAL", "131",        // CMP_VAL 131 (0x83 = right arrow)
            "JNZ", "70",             // JNZ draw (skip if not right arrow)
            "LOAD", "0xF2",          // LOAD current_x
            "ADD", "0xF8",           // ADD one
            "STORE", "0xF2",         // STORE current_x

            // draw: (address ~70)
            "LOAD", "0xF2",          // LOAD current_x
            "SET_X_FROM_ACC", "0",   // SET_X_FROM_ACC
            "LOAD", "0xF3",          // LOAD current_y
            "SET_Y_FROM_ACC", "0",   // SET_Y_FROM_ACC
            "SET_R", "255",          // SET_R 255 (red pixel)
            "SET_G", "0",            // SET_G 0
            "SET_B", "0",            // SET_B 0
            "DRAW_PIXEL", "0",       // DRAW_PIXEL

            // Check for ESC key to exit
            "LOAD", "0xF0",          // LOAD current_key
            "CMP_VAL", "27",         // CMP_VAL 27 (0x1B = ESC)
            "JNZ", "8",              // JNZ main_loop (jump back to address 8)

            "HLT", "0"               // HLT
        );
        // computer.Ram.ram[0xF5] = 32; computer.Ram.ram[0xF8] = 1;
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

