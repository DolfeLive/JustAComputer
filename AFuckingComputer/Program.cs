
namespace Computer;

public static class Program
{
    static void Main()
    {
        var computer = new Computer();
        computer.LoadProgram(new byte[]
        {
            0x01, 0x10, // LOAD 0x10 ;; loads 5 into mem
            0x03, 0x11, // ADD  0x11 ;; adds 3 to loaded 5
            0x02, 0x12, // STORE 0x12 ;; stores it in 0x12
            0xFF, 0x00  // HLT ;; stops
        });

        computer.Ram.ram[0x10] = 5;
        computer.Ram.ram[0x11] = 3;

        computer.Run();
    }
}
public class Computer
{
    public RAM Ram { get; }
    public CPU Cpu { get; }

    public Computer()
    {
        Ram = new RAM(256); // only 256 bytes
        Cpu = new CPU(Ram);
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
    private byte _acc = 0; // Accumulator (register)
    private byte _pc = 0; // Program Counter
    private bool _running = true;

    public CPU(RAM ram)
    {
        _ram = ram;
    }

    enum Opcode : byte
    {
        NOP = 0x00,   // Do nothing
        LOAD = 0x01,  // Load RAM[addr] into ACC
        STORE = 0x02, // Store ACC into RAM[addr]
        ADD = 0x03,   // ACC += RAM[addr]
        SUB = 0x04,   // ACC -= RAM[addr]
        JMP = 0x05,   // Jump to addr
        JZ = 0x06,    // Jump if ACC == 0
        HLT = 0xFF    // Halt program
    }

    public void Run()
    {
        while (_running)
        {
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
                    _acc += _ram[operand];
                    break;
                case Opcode.SUB:
                    _acc -= _ram[operand];
                    break;
                case Opcode.JMP:
                    _pc = operand;
                    break;
                case Opcode.JZ:
                    if (_acc == 0)
                        _pc = operand;
                    break;
                case Opcode.HLT:
                    _running = false;
                    break;
                default:
                    Console.WriteLine($"Unknown opcode: {opcode:X2} at address {_pc - 2:X2}");
                    _running = false;
                    break;
            }
        }

        Console.WriteLine($"Execution done. ACC = {_acc}");
    }
}
