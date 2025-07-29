
namespace Computer;

public static class Assembler
{
    private static readonly Dictionary<string, byte> OpcodeMap = new Dictionary<string, byte>
    {
        // CPU Instructions
        {"NOP", 0x00}, {"LOAD", 0x01}, {"STORE", 0x02}, {"ADD", 0x03}, {"SUB", 0x04},
        {"JMP", 0x05}, {"JZ", 0x06}, {"JNZ", 0x07}, {"CMP", 0x08}, {"CMP_VAL", 0x09}, {"HLT", 0x0F},
        
        // GPU Instructions
        {"SET_X", 0x10}, {"SET_Y", 0x11}, {"SET_R", 0x12}, {"SET_G", 0x13}, {"SET_B", 0x14},
        {"DRAW_PIXEL", 0x15}, {"CLEAR_SCREEN", 0x16}, {"SET_X_FROM_ACC", 0x17}, {"SET_Y_FROM_ACC", 0x18},
        {"SET_R_FROM_ACC", 0x19}, {"SET_G_FROM_ACC", 0x1A}, {"SET_B_FROM_ACC", 0x1B},
        
        // Input Instructions
        {"KEY_AVAILABLE", 0x20}, {"GET_KEY", 0x21}, {"PEEK_KEY", 0x22},
        
        // Logging Instructions
        {"LOG_ADD", 0x30}, {"LOG_LOAD", 0x31}, {"LOG_STORE", 0x32}, {"LOG_PRINT", 0x33},
    };

    public static byte[] Assemble(params string[] instructions)
    {
        var program = new List<byte>();

        for (int i = 0; i < instructions.Length; i += 2)
        {
            string opcode = instructions[i];
            byte operand = 0;

            if (i + 1 < instructions.Length)
            {
                if (byte.TryParse(instructions[i + 1], out operand)) { }
                else if (instructions[i + 1].StartsWith("0x"))
                {
                    // Hex
                    operand = Convert.ToByte(instructions[i + 1], 16);
                }
            }

            if (OpcodeMap.TryGetValue(opcode, out byte opcodeValue))
            {
                program.Add(opcodeValue);
                program.Add(operand);
            }
            else
            {
                throw new ArgumentException($"Unknown opcode: {opcode}");
            }
        }

        return program.ToArray();
    }
}

