using Computer;

namespace Computer;

public class CPU
{
    private readonly RAM _ram;
    private readonly GPU _gpu;
    private readonly Input _input;
    private readonly byte[] _log;
    private byte _acc = 0; // accumulator
    private ushort _pc = 0; // program counter (16-bit)
    private byte _logRegister = 0;
    private bool _running = true;

    public CPU(RAM ram, GPU gpu, Input input)
    {
        _ram = ram;
        _gpu = gpu;
        _input = input;
    }

    enum Opcode : byte
    {
        // Basic CPU Instructions (0x00-0x0F)
        NOP = 0x00,         // Do nothing
        LOAD = 0x01,        // Load RAM[addr] into ACC
        STORE = 0x02,       // Store ACC into RAM[addr]
        ADD = 0x03,         // ACC += RAM[addr]
        SUB = 0x04,         // ACC -= RAM[addr]
        JMP = 0x05,         // Jump to addr
        JZ = 0x06,          // Jump if ACC == 0
        JNZ = 0x07,         // Jump if ACC != 0
        CMP = 0x08,         // Compare ACC with RAM[addr]
        CMP_VAL = 0x09,     // Compare ACC with immediate value
        GT = 0x0A,          // ACC = (ACC > RAM[addr]) ? 1 : 0
        GT_VAL = 0x0B,      // ACC = (ACC > value) ? 1 : 0
        LT = 0x0C,          // ACC = (ACC < RAM[addr]) ? 1 : 0
        LT_VAL = 0x0D,      // ACC = (ACC < value) ? 1 : 0
        NOT = 0x0E,         // ACC = (ACC == 0) ? 1 : 0
        HLT = 0x0F,         // Halt

        // GPU Instructions (0x10-0x1F)
        SET_X = 0x10,           // Set GPU X register
        SET_Y = 0x11,           // Set GPU Y register 
        SET_R = 0x12,           // Set GPU R register
        SET_G = 0x13,           // Set GPU G register
        SET_B = 0x14,           // Set GPU B register
        DRAW_PIXEL = 0x15,      // Draw pixel at (X,Y) with current RGB
        CLEAR_SCREEN = 0x16,    // Clear the screen
        SET_X_FROM_ACC = 0x17,  // Set GPU X register from ACC
        SET_Y_FROM_ACC = 0x18,  // Set GPU Y register from ACC  
        SET_R_FROM_ACC = 0x19,  // Set GPU R register from ACC
        SET_G_FROM_ACC = 0x1A,  // Set GPU G register from ACC
        SET_B_FROM_ACC = 0x1B,  // Set GPU B register from ACC

        // Input Instructions (0x20-0x2F)
        KEY_AVAILABLE = 0x20,   // Set ACC to 1 if key available, 0 otherwise
        GET_KEY = 0x21,         // Get key from buffer into ACC
        PEEK_KEY = 0x22,        // Peek at next key without removing it

        // Logging Instructions (0x30-0x3F)
        LOG_ADD = 0x30,         // Add operand value to logging register
        LOG_LOAD = 0x31,        // Load logging register value into ACC
        LOG_STORE = 0x32,       // Store ACC value into logging register
        LOG_PRINT = 0x33,       // Print logging register in binary, hex, and string format
    }

    public void Run()
    {
        Console.WriteLine("\nComputer starting...");

        while (_running)
        {
            if (_pc >= _ram.ram.Length - 1) break;

            byte opcode = _ram[_pc++];   // fetch opcode
            byte operand = _ram[_pc++];  // fetch operand

            switch ((Opcode)opcode)
            {
                case Opcode.NOP:
                    print("Ran NOP");
                    break;

                case Opcode.LOAD:
                    print($"Ran LOAD set ACC from: {_acc}, to: {_ram[operand]}");
                    _acc = _ram[operand];
                    break;

                case Opcode.STORE:
                    print($"Ran STORE set RAM[{operand}] from: {_ram[operand]}, to: {_acc}");
                    _ram[operand] = _acc;
                    break;

                case Opcode.ADD:
                    print($"Ran ADD: {_acc} + {_ram[operand]} = {(byte)(_acc + _ram[operand])}");
                    _acc = (byte)(_acc + _ram[operand]);
                    break;

                case Opcode.SUB:
                    print($"Ran SUB: {_acc} - {_ram[operand]} = {(byte)(_acc - _ram[operand])}");
                    _acc = (byte)(_acc - _ram[operand]);
                    break;

                case Opcode.JMP:
                    print($"Ran JMP to address {operand}");
                    _pc = operand;
                    break;

                case Opcode.JZ:
                    print($"Ran JZ: ACC = {_acc}, Jumping: {_acc == 0}");
                    if (_acc == 0)
                        _pc = operand;
                    break;

                case Opcode.JNZ:
                    print($"Ran JNZ: ACC = {_acc}, Jumping: {_acc != 0}");
                    if (_acc != 0)
                        _pc = operand;
                    break;

                case Opcode.CMP:
                    print($"Ran CMP: ACC = {_acc}, RAM[{operand}] = {_ram[operand]}, Result = {(byte)(_acc != _ram[operand] ? 1 : 0)}");
                    _acc = (byte)(_acc != _ram[operand] ? 1 : 0);
                    break;

                case Opcode.CMP_VAL:
                    print($"Ran CMP_VAL: ACC = {_acc}, Value = {operand}, Result = {(byte)(_acc != operand ? 1 : 0)}");
                    _acc = (byte)(_acc != operand ? 1 : 0);
                    break;

                case Opcode.GT:
                    print($"Ran GT: ACC = {_acc}, RAM[{operand}] = {_ram[operand]}, Result = {(byte)(_acc > _ram[operand] ? 1 : 0)}");
                    _acc = (byte)(_acc > _ram[operand] ? 1 : 0);
                    break;

                case Opcode.GT_VAL:
                    print($"Ran GT_VAL: ACC = {_acc}, Value = {operand}, Result = {(byte)(_acc > operand ? 1 : 0)}");
                    _acc = (byte)(_acc > operand ? 1 : 0);
                    break;

                case Opcode.LT:
                    print($"Ran LT: ACC = {_acc}, RAM[{operand}] = {_ram[operand]}, Result = {(byte)(_acc < _ram[operand] ? 1 : 0)}");
                    _acc = (byte)(_acc < _ram[operand] ? 1 : 0);
                    break;

                case Opcode.LT_VAL:
                    print($"Ran LT_VAL: ACC = {_acc}, Value = {operand}, Result = {(byte)(_acc < operand ? 1 : 0)}");
                    _acc = (byte)(_acc < operand ? 1 : 0);
                    break;

                case Opcode.NOT:
                    print($"Ran NOT: ACC = {_acc}, Result = {(byte)(_acc == 0 ? 1 : 0)}");
                    _acc = (byte)(_acc == 0 ? 1 : 0);
                    break;

                case Opcode.HLT:
                    print("Ran HLT: Halting execution");
                    _running = false;
                    break;

                // GPU Instructions
                case Opcode.SET_X:
                    print($"Ran SET_X: {_gpu.X_REG} -> {operand}");
                    _gpu.X_REG = operand;
                    break;

                case Opcode.SET_X_FROM_ACC:
                    print($"Ran SET_X_FROM_ACC: {_gpu.X_REG} -> {_acc}");
                    _gpu.X_REG = _acc;
                    break;

                case Opcode.SET_Y:
                    print($"Ran SET_Y: {_gpu.Y_REG} -> {operand}");
                    _gpu.Y_REG = operand;
                    break;

                case Opcode.SET_Y_FROM_ACC:
                    print($"Ran SET_Y_FROM_ACC: {_gpu.Y_REG} -> {_acc}");
                    _gpu.Y_REG = _acc;
                    break;

                case Opcode.SET_R:
                    print($"Ran SET_R: {_gpu.R_REG} -> {operand}");
                    _gpu.R_REG = operand;
                    break;

                case Opcode.SET_R_FROM_ACC:
                    print($"Ran SET_R_FROM_ACC: {_gpu.R_REG} -> {_acc}");
                    _gpu.R_REG = _acc;
                    break;

                case Opcode.SET_G:
                    print($"Ran SET_G: {_gpu.G_REG} -> {operand}");
                    _gpu.G_REG = operand;
                    break;

                case Opcode.SET_G_FROM_ACC:
                    print($"Ran SET_G_FROM_ACC: {_gpu.G_REG} -> {_acc}");
                    _gpu.G_REG = _acc;
                    break;

                case Opcode.SET_B:
                    print($"Ran SET_B: {_gpu.B_REG} -> {operand}");
                    _gpu.B_REG = operand;
                    break;

                case Opcode.SET_B_FROM_ACC:
                    print($"Ran SET_B_FROM_ACC: {_gpu.B_REG} -> {_acc}");
                    _gpu.B_REG = _acc;
                    break;

                case Opcode.DRAW_PIXEL:
                    print("Ran DRAW_PIXEL");
                    _gpu.DrawPixel();
                    break;

                case Opcode.CLEAR_SCREEN:
                    print("Ran CLEAR_SCREEN");
                    _gpu.ClearScreen();
                    break;

                // Input Instructions
                case Opcode.KEY_AVAILABLE:
                    _acc = (byte)(_input.HasKey() ? 1 : 0);
                    print($"Ran KEY_AVAILABLE: ACC = {_acc}");
                    break;

                case Opcode.GET_KEY:
                    _acc = _input.GetKey();
                    print($"Ran GET_KEY: ACC = {_acc}");
                    break;

                case Opcode.PEEK_KEY:
                    _acc = _input.PeekKey();
                    print($"Ran PEEK_KEY: ACC = {_acc}");
                    break;

                // Logging Instructions
                case Opcode.LOG_ADD:
                    print($"Ran LOG_ADD: {_logRegister} + {operand} = {(byte)(_logRegister + operand)}");
                    _logRegister = (byte)(_logRegister + operand);
                    break;

                case Opcode.LOG_LOAD:
                    print($"Ran LOG_LOAD: ACC = {_logRegister}");
                    _acc = _logRegister;
                    break;

                case Opcode.LOG_STORE:
                    print($"Ran LOG_STORE: LogRegister = {_acc}");
                    _logRegister = _acc;
                    break;

                case Opcode.LOG_PRINT:
                    print("Ran LOG_PRINT:");
                    PrintLogRegister();
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
        Console.WriteLine($"Log register: {_logRegister}");
    }

    private void PrintLogRegister()
    {
        string binary = Convert.ToString(_logRegister, 2).PadLeft(8, '0');
        string hex = $"0x{_logRegister:X2}";
        string str = char.IsControl((char)_logRegister) ?
            $"[CTRL:{_logRegister}]" :
            ((char)_logRegister).ToString();

        Console.WriteLine($"LOG_REG: Binary={binary}, Hex={hex}, String='{str}', Decimal={_logRegister}");
    }

    void print(string str)
    {
        Console.WriteLine(str);
    }
}