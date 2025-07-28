using System.Drawing.Drawing2D;

namespace Computer;

public class PixelWindow : Form
{
    private Bitmap displayCanvas;
    private GPU gpu;
    private System.Windows.Forms.Timer refreshTimer;
    private const int PIXEL_SIZE = 8;
    private const int SCREEN_WIDTH = 64;
    private const int SCREEN_HEIGHT = 64;

    public PixelWindow(GPU gpu)
    {
        this.gpu = gpu;
        this.Text = "Display, bitch";

        int clientWidth = SCREEN_WIDTH * PIXEL_SIZE;
        int clientHeight = SCREEN_HEIGHT * PIXEL_SIZE;

        this.ClientSize = new Size(clientWidth, clientHeight);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;

        displayCanvas = new Bitmap(clientWidth, clientHeight);

        // Enable double buffering and custom paint
        this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer, true);

        refreshTimer = new System.Windows.Forms.Timer();
        refreshTimer.Interval = 16; // ~60 FPS
        refreshTimer.Tick += (s, e) => RefreshDisplay();
        refreshTimer.Start();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
        e.Graphics.DrawImage(displayCanvas, 0, 0);
    }

    private void RefreshDisplay()
    {
        if (gpu.VideoMemoryDirty)
        {
            using (Graphics g = Graphics.FromImage(displayCanvas))
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;

                for (int y = 0; y < SCREEN_HEIGHT; y++)
                {
                    for (int x = 0; x < SCREEN_WIDTH; x++)
                    {
                        var color = gpu.GetPixelColor(x, y);
                        using (var brush = new SolidBrush(color))
                        {
                            g.FillRectangle(brush,
                                x * PIXEL_SIZE,
                                y * PIXEL_SIZE,
                                PIXEL_SIZE,
                                PIXEL_SIZE);
                        }
                    }
                }
            }
            this.Invalidate();
            gpu.VideoMemoryDirty = false;
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        refreshTimer?.Stop();
        refreshTimer?.Dispose();
        displayCanvas?.Dispose();
        base.OnFormClosed(e);
    }
}

public static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();

        var computer = new Computer();
        var window = new PixelWindow(computer.Gpu);

        // Drawing to screen
        computer.LoadProgram(new byte[]
        {
            // set (10, 10) to red
            0x10, 10,    // SET_X 10
            0x11, 10,    // SET_Y 10  
            0x12, 255,   // SET_R 255 (red)
            0x13, 0,     // SET_G 0
            0x14, 0,     // SET_B 0
            0x15, 0x00,  // DRAW_PIXEL
            
            // set (20, 20) to green
            0x10, 20,    // SET_X 20
            0x11, 20,    // SET_Y 20
            0x12, 0,     // SET_R 0
            0x13, 255,   // SET_G 255 (green)
            0x14, 0,     // SET_B 0
            0x15, 0x00,  // DRAW_PIXEL
            
            // set (30, 30) to blue
            0x10, 30,    // SET_X 30
            0x11, 30,    // SET_Y 30
            0x12, 0,     // SET_R 0
            0x13, 0,     // SET_G 0
            0x14, 255,   // SET_B 255 (blue)
            0x15, 0x00,  // DRAW_PIXEL
            
            0xFF, 0x00   // HLT
        });

        //// Set mem values and add em together
        //computer.LoadProgram(new byte[]
        //{
        //    0x01, 0x10, // LOAD 0x10 ;; loads 5 into mem
        //    0x03, 0x11, // ADD  0x11 ;; adds 3 to loaded 5
        //    0x02, 0x12, // STORE 0x12 ;; stores it in 0x12
        //    0xFF, 0x00  // HLT ;; stops
        //});

        //computer.Ram.ram[0x10] = 5;
        //computer.Ram.ram[0x11] = 3;

        var computerThread = new Thread(() => computer.Run());
        computerThread.IsBackground = true;
        computerThread.Start();

        Application.Run(window);
    }
}

public class Computer
{
    public RAM Ram { get; }
    public CPU Cpu { get; }
    public GPU Gpu { get; }

    public Computer()
    {
        Ram = new RAM(4096); // More memory - 4KB
        Gpu = new GPU();
        Cpu = new CPU(Ram, Gpu);
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

public class GPU
{
    public byte X_REG { get; set; } = 0;    // X coordinate
    public byte Y_REG { get; set; } = 0;    // Y coordinate  
    public byte R_REG { get; set; } = 0;    // Red
    public byte G_REG { get; set; } = 0;    // Green
    public byte B_REG { get; set; } = 0;    // Blue

    public const int XWidth = 64;
    public const int YWidth = 64;

    // VMEM: XWidthxYWidth pixels, 3 bytes per pixel (RGB)
    private byte[] videoMemory = new byte[XWidth * YWidth * 3];
    public bool VideoMemoryDirty { get; set; } = false;

    public void DrawPixel()
    {
        if (X_REG < XWidth && Y_REG < YWidth)
        {
            int offset = (Y_REG * XWidth + X_REG) * 3;
            videoMemory[offset] = R_REG;     // Red
            videoMemory[offset + 1] = G_REG; // Green  
            videoMemory[offset + 2] = B_REG; // Blue
            VideoMemoryDirty = true;
        }
    }

    public Color GetPixelColor(int x, int y)
    {
        if (x >= XWidth || y >= YWidth) return Color.Black;

        int offset = (y * XWidth + x) * 3;
        byte r = videoMemory[offset];
        byte g = videoMemory[offset + 1];
        byte b = videoMemory[offset + 2];
        return Color.FromArgb(255, r, g, b); // Fully alpha
    }

    public void ClearScreen()
    {
        Array.Clear(videoMemory, 0, videoMemory.Length);
        VideoMemoryDirty = true;
    }
}

public class RAM
{
    public byte[] ram;

    public RAM(int size)
    {
        ram = new byte[size];
    }

    public byte this[int index]
    {
        get => ram[index];
        set => ram[index] = value;
    }
}

public class CPU
{
    private readonly RAM _ram;
    private readonly GPU _gpu;
    private byte _acc = 0; // Accumulator
    private ushort _pc = 0; // Program Counter (16-bit for more memory)
    private bool _running = true;

    public CPU(RAM ram, GPU gpu)
    {
        _ram = ram;
        _gpu = gpu;
    }

    enum Opcode : byte
    {
        // CPU Instruct
        NOP = 0x00,     // Do nothing
        LOAD = 0x01,    // Load RAM[addr] into ACC
        STORE = 0x02,   // Store ACC into RAM[addr]
        ADD = 0x03,     // ACC += RAM[addr]
        SUB = 0x04,     // ACC -= RAM[addr]
        JMP = 0x05,     // Jump to addr
        JZ = 0x06,      // Jump if ACC == 0
        JNZ = 0x07,     // Jump if ACC != 0
        CMP = 0x08,     // Compare ACC with RAM[addr]

        // GPU Instruct
        SET_X = 0x10,       // Set GPU X register
        SET_Y = 0x11,       // Set GPU Y register  
        SET_R = 0x12,       // Set GPU R register
        SET_G = 0x13,       // Set GPU G register
        SET_B = 0x14,       // Set GPU B register
        DRAW_PIXEL = 0x15,  // Draw pixel at (X,Y) with current RGB
        CLEAR_SCREEN = 0x16, // Clear the screen

        HLT = 0xFF      // Halt
    }

    public void Run()
    {
        Console.WriteLine("Computer starting...");

        while (_running)
        {
            if (_pc >= _ram.ram.Length - 1) break;

            byte opcode = _ram[_pc++];   // Fetch opcode
            byte operand = _ram[_pc++];  // Fetch operand

            switch ((Opcode)opcode)
            {
                case Opcode.NOP:
                    break;

                case Opcode.LOAD:
                    _acc = _ram[operand];
                    break;

                case Opcode.STORE:
                    _ram[operand] = _acc;
                    break;

                case Opcode.ADD:
                    _acc = (byte)(_acc + _ram[operand]);
                    break;

                case Opcode.SUB:
                    _acc = (byte)(_acc - _ram[operand]);
                    break;

                case Opcode.JMP:
                    _pc = operand;
                    break;

                case Opcode.JZ:
                    if (_acc == 0)
                        _pc = operand;
                    break;

                case Opcode.JNZ:
                    if (_acc != 0)
                        _pc = operand;
                    break;

                case Opcode.CMP:
                    // Sets _acc to comparison result
                    _acc = (byte)(_acc == _ram[operand] ? 1 : 0);
                    break;

                // GPU Instructions
                case Opcode.SET_X:
                    _gpu.X_REG = operand;
                    break;

                case Opcode.SET_Y:
                    _gpu.Y_REG = operand;
                    break;

                case Opcode.SET_R:
                    _gpu.R_REG = operand;
                    break;

                case Opcode.SET_G:
                    _gpu.G_REG = operand;
                    break;

                case Opcode.SET_B:
                    _gpu.B_REG = operand;
                    break;

                case Opcode.DRAW_PIXEL:
                    _gpu.DrawPixel();
                    break;

                case Opcode.CLEAR_SCREEN:
                    _gpu.ClearScreen();
                    break;

                case Opcode.HLT:
                    _running = false;
                    break;

                default:
                    Console.WriteLine($"Unknown opcode: {opcode:X2} at address {_pc - 2:X4}");
                    _running = false;
                    break;
            }

            Thread.Sleep(16);
        }

        Console.WriteLine($"Execution complete. ACC = {_acc}");
        Console.WriteLine($"GPU registers: X={_gpu.X_REG}, Y={_gpu.Y_REG}, R={_gpu.R_REG}, G={_gpu.G_REG}, B={_gpu.B_REG}");
    }
}