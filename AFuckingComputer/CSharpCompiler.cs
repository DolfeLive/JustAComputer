using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Computer;

public class CSharpCompiler
{
    // CPU Opcodes matching your CPU implementation
    private enum Opcode : byte
    {
        NOP = 0x00, LOAD = 0x01, STORE = 0x02, ADD = 0x03, SUB = 0x04,
        JMP = 0x05, JZ = 0x06, JNZ = 0x07, CMP = 0x08, CMP_VAL = 0x09,
        // New comparison instructions
        GT = 0x0A,          // ACC = (ACC > RAM[addr]) ? 1 : 0
        GT_VAL = 0x0B,      // ACC = (ACC > value) ? 1 : 0
        LT = 0x0C,          // ACC = (ACC < RAM[addr]) ? 1 : 0
        LT_VAL = 0x0D,      // ACC = (ACC < value) ? 1 : 0
        NOT = 0x0E,         // ACC = (ACC == 0) ? 1 : 0
        HLT = 0x0F,
        SET_X = 0x10, SET_Y = 0x11, SET_R = 0x12, SET_G = 0x13, SET_B = 0x14,
        DRAW_PIXEL = 0x15, CLEAR_SCREEN = 0x16,
        SET_X_FROM_ACC = 0x17, SET_Y_FROM_ACC = 0x18,
        SET_R_FROM_ACC = 0x19, SET_G_FROM_ACC = 0x1A, SET_B_FROM_ACC = 0x1B,
        KEY_AVAILABLE = 0x20, GET_KEY = 0x21, PEEK_KEY = 0x22,
        LOG_ADD = 0x30, LOG_LOAD = 0x31, LOG_STORE = 0x32, LOG_PRINT = 0x33
    }

    private Dictionary<string, byte> _variables = new Dictionary<string, byte>();
    private Dictionary<byte, byte> _constants = new Dictionary<byte, byte>(); // value -> address
    private Dictionary<string, ushort> _labels = new Dictionary<string, ushort>();
    private List<byte> _bytecode = new List<byte>();
    private byte _nextVarAddress = 200; // start variables at address 200
    private byte _nextConstAddress = 100; // start constants at address 100
    private ushort _currentAddress = 0;
    private List<(ushort address, string label)> _unresolvedJumps = new List<(ushort, string)>();

    public byte[] Compile(string csharpCode)
    {
        string cleanCode = PreprocessCode(csharpCode);
        ParseAndCompile(cleanCode);
        ResolveJumps();
        return CreateFinalBytecode();
    }

    private byte[] CreateFinalBytecode()
    {
        byte[] memory = new byte[256]; // full 8bit address space

        // init constants
        foreach (var kvp in _constants)
        {
            byte value = kvp.Key;
            byte address = kvp.Value;
            memory[address] = value;
        }

        // copy program bytecode starting from address 0
        for (int i = 0; i < _bytecode.Count; i++)
        {
            memory[i] = _bytecode[i];
        }

        return memory;
    }

    public byte[] GetProgramBytecode()
    {
        return _bytecode.ToArray();
    }

    public Dictionary<byte, byte> GetConstants()
    {
        return new Dictionary<byte, byte>(_constants);
    }

    private byte GetOrCreateConstant(byte value)
    {
        if (!_constants.ContainsKey(value))
        {
            _constants[value] = _nextConstAddress++;
        }
        return _constants[value];
    }

    private string PreprocessCode(string code)
    {
        code = Regex.Replace(code, @"//.*", ""); // remove single line comments
        code = Regex.Replace(code, @"/\*.*?\*/", "", RegexOptions.Singleline); // remove multi line comments
        code = Regex.Replace(code, @"\s+", " "); // normalize whitespace
        code = code.Trim();

        return code;
    }

    private void ParseAndCompile(string code)
    {
        Console.WriteLine($"In code: {code}");
        var mainMatch = Regex.Match(code, @"static\s+void\s+Main\s*\(\s*\)\s*\{(.*)\}", RegexOptions.Singleline);
        if (!mainMatch.Success)
        {
            throw new Exception("No Main method found");
        }

        string mainBody = mainMatch.Groups[1].Value.Trim();
        CompileStatements(mainBody);
        Console.WriteLine($"Out code: {mainBody}");
        EmitInstruction(Opcode.HLT, 0);
    }

    private void CompileStatements(string code)
    {
        var statements = SplitStatements(code);

        foreach (string statement in statements)
        {
            var trimmed = statement.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                Console.WriteLine($"Compiling statement: {trimmed}");
                CompileStatement(trimmed);
            }
        }
    }

    private List<string> SplitStatements(string code)
    {
        var statements = new List<string>();
        var current = "";
        int braceLevel = 0;
        bool inString = false;
        int i = 0;

        while (i < code.Length)
        {
            char c = code[i];

            if (c == '"' && (i == 0 || code[i - 1] != '\\'))
                inString = !inString;

            current += c;

            if (!inString)
            {
                if (c == '{')
                {
                    braceLevel++;
                }
                else if (c == '}')
                {
                    braceLevel--;

                    // check if we have just completed a control structure
                    if (braceLevel == 0)
                    {
                        var trimmed = current.Trim();
                        if (trimmed.StartsWith("if") || trimmed.StartsWith("while"))
                        {
                            statements.Add(trimmed);
                            current = "";
                            i++;
                            continue;
                        }
                    }
                }
                else if (c == ';' && braceLevel == 0)
                {
                    // simple statement ended
                    var stmt = current.Trim();
                    if (!string.IsNullOrEmpty(stmt))
                        statements.Add(stmt);
                    current = "";
                    i++;
                    continue;
                }
            }

            i++;
        }

        if (!string.IsNullOrWhiteSpace(current))
        {
            statements.Add(current.Trim());
        }

        return statements;
    }

    private void CompileStatement(string statement)
    {
        if (string.IsNullOrWhiteSpace(statement)) return;

        // if statements
        if (statement.StartsWith("if"))
        {
            CompileIfStatement(statement);
            return;
        }

        // while loops
        if (statement.StartsWith("while"))
        {
            CompileWhileLoop(statement);
            return;
        }

        // variable declaration: byte varName = value;
        var varDeclMatch = Regex.Match(statement, @"byte\s+(\w+)\s*=\s*(.+)");
        if (varDeclMatch.Success)
        {
            string varName = varDeclMatch.Groups[1].Value;
            string expression = varDeclMatch.Groups[2].Value.TrimEnd(';');

            byte address = AllocateVariable(varName);
            CompileExpression(expression);
            EmitInstruction(Opcode.STORE, address);
            return;
        }

        // assignment: varName = value;
        var assignMatch = Regex.Match(statement, @"(\w+)\s*=\s*(.+)");
        if (assignMatch.Success)
        {
            string varName = assignMatch.Groups[1].Value;
            string expression = assignMatch.Groups[2].Value;

            if (!_variables.ContainsKey(varName))
                AllocateVariable(varName);

            CompileExpression(expression);
            EmitInstruction(Opcode.STORE, _variables[varName]);
            return;
        }

        // method calls
        if (statement.Contains("(") && statement.Contains(")"))
        {
            CompileMethodCall(statement);
            return;
        }
    }

    private void CompileExpression(string expression)
    {

        expression = expression.Trim();
        expression = expression.Trim().TrimEnd(';');
        Console.WriteLine("expr: " + expression);

        // simple number: create constant and load it
        if (byte.TryParse(expression, out byte value))
        {
            byte constAddr = GetOrCreateConstant(value);
            EmitInstruction(Opcode.LOAD, constAddr);
            return;
        }

        // variable
        if (_variables.ContainsKey(expression))
        {
            EmitInstruction(Opcode.LOAD, _variables[expression]);
            return;
        }

        // binary operations
        var addMatch = Regex.Match(expression, @"(.+?)\s*\+\s*(.+)");
        if (addMatch.Success)
        {
            string left = addMatch.Groups[1].Value.Trim();
            string right = addMatch.Groups[2].Value.Trim();

            CompileExpression(left);
            EmitInstruction(Opcode.STORE, 255); // temp storage
            CompileExpression(right);
            EmitInstruction(Opcode.ADD, 255); // add temp to ACC
            return;
        }

        var subMatch = Regex.Match(expression, @"(.+?)\s*-\s*(.+)");
        if (subMatch.Success)
        {
            string left = subMatch.Groups[1].Value.Trim();
            string right = subMatch.Groups[2].Value.Trim();

            CompileExpression(left);            // load left operand into ACC
            EmitInstruction(Opcode.STORE, 254); // store left in temp1
            CompileExpression(right);           // load right operand into ACC  
            EmitInstruction(Opcode.STORE, 255); // store right in temp2
            EmitInstruction(Opcode.LOAD, 254);  // load left back into ACC
            EmitInstruction(Opcode.SUB, 255);   // ACC = left - right
            return;
        }
    }

    private void CompileMethodCall(string statement)
    {
        // GPU methods
        if (statement.Contains("SetPixel("))
        {
            var match = Regex.Match(statement, @"SetPixel\s*\(\s*(.+?)\s*,\s*(.+?)\s*,\s*(.+?)\s*,\s*(.+?)\s*,\s*(.+?)\s*\)");
            if (match.Success)
            {
                string xExpr = match.Groups[1].Value.Trim();
                string yExpr = match.Groups[2].Value.Trim();
                string rExpr = match.Groups[3].Value.Trim();
                string gExpr = match.Groups[4].Value.Trim();
                string bExpr = match.Groups[5].Value.Trim();

                // handle each param - could be variable or constant
                CompileSetPixelParam(xExpr, Opcode.SET_X, Opcode.SET_X_FROM_ACC);
                CompileSetPixelParam(yExpr, Opcode.SET_Y, Opcode.SET_Y_FROM_ACC);
                CompileSetPixelParam(rExpr, Opcode.SET_R, Opcode.SET_R_FROM_ACC);
                CompileSetPixelParam(gExpr, Opcode.SET_G, Opcode.SET_G_FROM_ACC);
                CompileSetPixelParam(bExpr, Opcode.SET_B, Opcode.SET_B_FROM_ACC);

                EmitInstruction(Opcode.DRAW_PIXEL, 0);
            }
            return;
        }

        if (statement.Contains("ClearScreen()"))
        {
            EmitInstruction(Opcode.CLEAR_SCREEN, 0);
            return;
        }

        // input methods
        if (statement.Contains("GetKey()"))
        {
            EmitInstruction(Opcode.GET_KEY, 0);
            return;
        }

        // logging methods
        if (statement.Contains("LogPrint()"))
        {
            EmitInstruction(Opcode.LOG_PRINT, 0);
            return;
        }
    }

    private void CompileSetPixelParam(string expr, Opcode directOpcode, Opcode fromAccOpcode)
    {
        if (byte.TryParse(expr, out byte value))
        {
            // direct constant value
            EmitInstruction(directOpcode, value);
        }
        else if (_variables.ContainsKey(expr))
        {
            // variable: load into ACC first and then set from ACC
            EmitInstruction(Opcode.LOAD, _variables[expr]);
            EmitInstruction(fromAccOpcode, 0);
        }
        else
        {
            // expression: compile it and then set from ACC
            CompileExpression(expr);
            EmitInstruction(fromAccOpcode, 0);
        }
    }

    private void CompileIfStatement(string statement)
    {
        // if (condition) { body }
        var match = Regex.Match(statement, @"if\s*\(\s*(.+?)\s*\)\s*\{([^{}]*(?:\{[^}]*\}[^{}]*)*)\}");
        if (match.Success)
        {
            string condition = match.Groups[1].Value;
            string body = match.Groups[2].Value;

            Console.WriteLine($"Compiling if: condition='{condition}', body='{body}'");

            CompileCondition(condition);

            string skipLabel = $"skip_{_currentAddress}";
            EmitJump(Opcode.JZ, skipLabel); // jump if condition is false (ACC == 0)

            CompileStatements(body);

            AddLabel(skipLabel);
        }
    }

    private void CompileWhileLoop(string statement)
    {
        // while (condition) { body }
        var match = Regex.Match(statement, @"while\s*\(\s*(.+?)\s*\)\s*\{([^{}]*(?:\{[^}]*\}[^{}]*)*)\}");
        if (match.Success)
        {
            string condition = match.Groups[1].Value;
            string body = match.Groups[2].Value;

            Console.WriteLine($"Compiling while: condition='{condition}', body='{body}'");

            string loopStart = $"loop_{_currentAddress}";
            string loopEnd = $"end_{_currentAddress}";

            AddLabel(loopStart);
            CompileCondition(condition);
            EmitJump(Opcode.JZ, loopEnd); // exit if condition is false

            CompileStatements(body);
            EmitJump(Opcode.JMP, loopStart); // jump back to start

            AddLabel(loopEnd);
        }
    }

    private void CompileCondition(string condition)
    {
        // simple comparisons
        var eqMatch = Regex.Match(condition, @"(.+?)\s*==\s*(.+)");
        if (eqMatch.Success)
        {
            string left = eqMatch.Groups[1].Value.Trim();
            string right = eqMatch.Groups[2].Value.Trim();

            CompileExpression(left);
            if (byte.TryParse(right, out byte value))
            {
                EmitInstruction(Opcode.CMP_VAL, value);
            }
            else if (_variables.ContainsKey(right))
            {
                EmitInstruction(Opcode.CMP, _variables[right]);
            }
            // CMP returns 1 if different, 0 if same
            // For ==, I want 1 if same (true), 0 if different (false)
            // So we use NOT to flip: 0->1, 1->0
            EmitInstruction(Opcode.NOT, 0);
            return;
        }

        var neMatch = Regex.Match(condition, @"(.+?)\s*!=\s*(.+)");
        if (neMatch.Success)
        {
            string left = neMatch.Groups[1].Value.Trim();
            string right = neMatch.Groups[2].Value.Trim();

            CompileExpression(left);
            if (byte.TryParse(right, out byte value))
            {
                EmitInstruction(Opcode.CMP_VAL, value);
            }
            else if (_variables.ContainsKey(right))
            {
                EmitInstruction(Opcode.CMP, _variables[right]);
            }
            // For !=: CMP gives 1 if different (true), 0 if same (false) - this is what we want
            return;
        }

        var gtMatch = Regex.Match(condition, @"(.+?)\s*>\s*(.+)");
        if (gtMatch.Success)
        {
            string left = gtMatch.Groups[1].Value.Trim();
            string right = gtMatch.Groups[2].Value.Trim();

            CompileExpression(left);
            if (byte.TryParse(right, out byte value))
            {
                EmitInstruction(Opcode.GT_VAL, value);
            }
            else if (_variables.ContainsKey(right))
            {
                EmitInstruction(Opcode.GT, _variables[right]);
            }
            return;
        }

        var ltMatch = Regex.Match(condition, @"(.+?)\s*<\s*(.+)");
        if (ltMatch.Success)
        {
            string left = ltMatch.Groups[1].Value.Trim();
            string right = ltMatch.Groups[2].Value.Trim();

            CompileExpression(left);
            if (byte.TryParse(right, out byte value))
            {
                EmitInstruction(Opcode.LT_VAL, value);
            }
            else if (_variables.ContainsKey(right))
            {
                EmitInstruction(Opcode.LT, _variables[right]);
            }
            return;
        }

        var geMatch = Regex.Match(condition, @"(.+?)\s*>=\s*(.+)");
        if (geMatch.Success)
        {
            string left = geMatch.Groups[1].Value.Trim();
            string right = geMatch.Groups[2].Value.Trim();

            // A >= B is == to !(A < B)
            CompileExpression(left);
            if (byte.TryParse(right, out byte value))
            {
                EmitInstruction(Opcode.LT_VAL, value);
            }
            else if (_variables.ContainsKey(right))
            {
                EmitInstruction(Opcode.LT, _variables[right]);
            }
            EmitInstruction(Opcode.NOT, 0);
            return;
        }

        var leMatch = Regex.Match(condition, @"(.+?)\s*<=\s*(.+)");
        if (leMatch.Success)
        {
            string left = leMatch.Groups[1].Value.Trim();
            string right = leMatch.Groups[2].Value.Trim();

            // A <= B is equivalent to !(A > B)
            CompileExpression(left);
            if (byte.TryParse(right, out byte value))
            {
                EmitInstruction(Opcode.GT_VAL, value);
            }
            else if (_variables.ContainsKey(right))
            {
                EmitInstruction(Opcode.GT, _variables[right]);
            }
            EmitInstruction(Opcode.NOT, 0);
            return;
        }
    }

    private byte AllocateVariable(string name)
    {
        if (!_variables.ContainsKey(name))
        {
            _variables[name] = _nextVarAddress++;
        }
        return _variables[name];
    }

    private void EmitInstruction(Opcode opcode, byte operand)
    {
        _bytecode.Add((byte)opcode);
        _bytecode.Add(operand);
        _currentAddress += 2;
    }

    private void EmitJump(Opcode jumpOpcode, string label)
    {
        _unresolvedJumps.Add(((ushort)(_currentAddress + 1), label));
        EmitInstruction(jumpOpcode, 0); // placeholder
    }

    private void AddLabel(string label)
    {
        _labels[label] = _currentAddress;
    }

    private void ResolveJumps()
    {
        foreach (var (address, label) in _unresolvedJumps)
        {
            if (_labels.ContainsKey(label))
            {
                _bytecode[address] = (byte)_labels[label];
            }
        }
    }

    public string DisassembleBytecode(byte[] memory)
    {
        var result = new List<string>();

        result.Add("=== CONSTANTS ===");
        foreach (var kvp in _constants)
        {
            result.Add($"CONST[{kvp.Value:X2}] = {kvp.Key}");
        }

        result.Add("\n=== PROGRAM ===");

        for (int i = 0; i < _bytecode.Count; i += 2)
        {
            if (i + 1 >= _bytecode.Count) break;

            byte opcode = _bytecode[i];
            byte operand = _bytecode[i + 1];

            string opcodeStr = Enum.IsDefined(typeof(Opcode), opcode)
                ? ((Opcode)opcode).ToString()
                : $"UNKNOWN({opcode:X2})";

            result.Add($"{i:X4}: {opcodeStr} {operand}");
        }

        return string.Join("\n", result);
    }
}