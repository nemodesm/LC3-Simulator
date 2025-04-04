using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace LC3_Simulator;

// custom instruction set
// loc [address] -----------> set arbitrary location in memory as current
// data [value (16 bits)] --> set arbitrary value in memory

public static class Compiler
{
    private static int _currentLineIndex = 0;

    private static string _currentLine = "";
    
    private static Simulator _simulator = new();

    public static Dictionary<string, Func<string[], (int nWords, LC3Instruction[]? words)>> PseudoInstructionParserTable =
        new()
        {
            { ".ORIG", ParseOrigin },
            { ".BLKW", ParseData },
            { ".FILL", ParseFill },
            { ".STRINGZ", ParseString },
            { ".END", ParseEnd }
        };

    public static Dictionary<string, Func<string[], LC3Instruction>> InstructionParserTable = new()
    {
        { "BR", ParseBr },
        { "BRN", ParseBr },
        { "BRZ", ParseBr },
        { "BRP", ParseBr },
        { "BRNZ", ParseBr },
        { "BRZP", ParseBr },
        { "BRNP", ParseBr },
        { "BRNZP", ParseBr },
        { "ADD", ParseAdd },
        { "LD", ParseLd },
        { "ST", ParseSt },
        { "JSR", ParseJsr },
        { "JSRR", ParseJsrr },
        { "AND", ParseAnd },
        { "LDR", ParseLdr },
        { "STR", ParseStr },
        { "RTI", ParseRti },
        { "NOT", ParseNot },
        { "LDI", ParseLdi },
        { "STI", ParseSti },
        { "JMP", ParseJmp },
        { "RET", ParseRet },
        { "LEA", ParseLea },
        { "TRAP", ParseTrap }
    };

    public static void RegisterMessage(string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        if (_currentLine != "")
        {
            Console.WriteLine($"Near line {_currentLineIndex + 1} --> {_currentLine.Trim()}");
        }
        Console.ResetColor();
    }
    
    public static void RegisterWarning(string message)
    {
        RegisterMessage($"Warning: {message}", ConsoleColor.Yellow);
    }
    
    public static void RegisterError(string message)
    {
        RegisterMessage($"Error: {message}", ConsoleColor.Red);
    }
    
    [Pure]
    public static ushort ParseNumber(string number, [ValueRange(0, 16)] byte bits)
    {
        var mask = (1 << bits) - 1;
        uint parsed;
        try
        {
            parsed = number[1] switch
            {
                '$' => uint.Parse(number[2..], System.Globalization.NumberStyles.HexNumber),
                'b' => uint.Parse(number[2..], System.Globalization.NumberStyles.BinaryNumber),
                _ => (uint)int.Parse(number[1..])
            };
        }
        catch (FormatException e)
        {
            throw new FormatException($"Could not parse number {number}: Invalid Format", e);
        }
        catch (OverflowException e)
        {
            throw new FormatException($"Could not parse number {number}: Negative number in unexpected place", e);
        }
        if (parsed > mask && number[1] != '-')
        {
            RegisterWarning($"Number {number} (parsed to {parsed}) is too large for {bits} bits, this can cause unexpected behavior");
        }
        return (ushort)(parsed & mask);
    }
    
    [Pure]
    public static byte GetRegisterCode(string register)
    {
        var res = (byte)(register[1] - '0');
        if (res > 7)
        {
            throw new FormatException($"Register {register} does not exist");
        }
        return res;
    }

    [Pure]
    public static bool IsAnyInstruction(string token)
    {
        return IsLC3Instruction(token) || IsPseudoInstruction(token);
    }

    [Pure]
    public static bool IsLC3Instruction(string token)
    {
        return token is "BR" or "BRN" or "BRZ" or "BRP" or "BRNZ" or "BRZP" or "BRNP" or "BRNZP"
            or "ADD" or "LD" or "ST" or "JSR" or "JSRR" or "AND" or "LDR" or "STR" or "RTI" or "NOT" or "LDI" or "STI"
            or "JMP" or "RET" or "LEA" or "TRAP";
    }
    
    public static bool IsPseudoInstruction(string token)
    {
        return token is ".ORIG" or ".BLKW" or ".FILL" or ".STRINGZ" or ".END";
    }

    [Pure]
    public static bool IsLabelValid(string label)
    {
        if (label is "R0" or "R1" or "R2" or "R3" or "R4" or "R5" or "R6" or "R7")
        {
            return false;
        }

        if (IsAnyInstruction(label))
        {
            return false;
        }

        for (var index = 0; index < label.Length; index++)
        {
            var c = label[index];
            if (!char.IsLetterOrDigit(c) && !(index is 0 && c == '\\'))
            {
                return false;
            }
        }

        return true;
    }
    
    [Pure]
    public static bool IsLineEmpty(string line) => line.TrimEnd() == "";

    public static ushort GetDataBlockSize(string dataBlock)
    {
        var tokens = dataBlock.Split(' ');
        switch (tokens[0])
        {
            case ".ORIG":
                return 0;
            case ".STRINGZ":
                return (ushort)(tokens[1].Length + 1);
            case ".FILL":
                return 1;
            case ".BLKW":
                return ushort.Parse(tokens[1]);
            case ".END":
                return 0;
            default:
                throw new FormatException("Invalid data block");
        }
    }

    [Pure]
    public static bool GetAddressFromCustomInstruction(string instruction, [NotNullWhen(true)] out ushort? address)
    {
        address = null;
        var tokens = instruction.Split(' ');
        if (tokens.Length != 2)
        {
            return false;
        }
        
        if (tokens[0] != "loc")
        {
            return false;
        }
        
        if (tokens[1][0] != '#')
        {
            return false;
        }
        
        address = ParseNumber(tokens[1], 16);

        return true;
    }

    [Pure]
    public static string[] StripComments(string[] lines)
    {
        var newLines = new string[lines.Length];
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            var commentIndex = line.IndexOf(';');
            if (commentIndex != -1)
            {
                line = line[..commentIndex];
            }
            newLines[index] = line;
        }
        return newLines;
    }

    [Pure]
    public static string[] StripLabels(string[] lines)
    {
        var newLines = new string[lines.Length];
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            if (line[0] == ' ')
            {
                newLines[index] = line;
                continue;
            }
            var labelIndex = line.IndexOf(':');
            if (labelIndex != -1)
            {
                line = line[(labelIndex + 1)..];
            }
            newLines[index] = line;
        }
        return newLines;
    }

    [Pure]
    public static string[] RemoveEmptyLines(string[] lines)
    {
        var newLines = new string[lines.Length];
        var index = 0;
        foreach (var line in lines)
        {
            if (line.TrimEnd() != "")
            {
                newLines[index++] = line;
            }
        }
        return newLines[..index];
    }

    [Pure]
    public static bool ParseLabels(string[] lines)
    {
        var labels = new Dictionary<string, int>();
        var lastLabel = "";
        var address = 0;
        var valid = true;
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            if (line[0] == ' ')
            {
                if (GetAddressFromCustomInstruction(line.Trim(), out var newAddress))
                {
                    address = newAddress.Value;
                    line = "";
                }
                goto EndOfLoop;
            }
            var labelIndex = line.IndexOf(':');
            if (labelIndex != -1)
            {
                var label = line[..labelIndex];
                var restOfLine = line[(labelIndex + 1)..];
                if (GetAddressFromCustomInstruction(restOfLine, out var newAddress))
                {
                    address = newAddress.Value;
                    restOfLine = ""; // force restOfLine to empty string to not increment address at end of loop
                }
                if (IsLabelValid(label))
                {
                    labels[label] = address;
                    lastLabel = label;
                }
                else
                {
                    RegisterError($"Label {label} is not in valid format");
                    valid = false;
                }

                line = restOfLine;
            }
            else
            {
                RegisterError("Label not followed by colon");
                valid = false;
            }

            EndOfLoop:
            if (!IsLineEmpty(line))
            {
                ++address;
            }
        }

        if (!valid)
        {
            return false;
        }
        
        // apply labels
        address = 0;
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index].TrimEnd();
            if (GetAddressFromCustomInstruction(line.Trim(), out var newAddress))
            {
                address = newAddress.Value;
                line = "";
                goto EndOfLoop;
            }
            foreach (var label in labels)
            {
                if (line.Contains($",{label.Key},"))
                {
                    Console.WriteLine($"Replacing {label.Key} with #${label.Value - address:X} ({label.Value:X} - {address:X})");
                    line = line.Replace($",{label.Key},", $",#${label.Value - address:X},");
                }
                else if (line.EndsWith($",{label.Key}"))
                {
                    Console.WriteLine($"Replacing {label.Key} with #${label.Value - address:X} ({label.Value:X} - {address:X})");
                    line = line.Replace($",{label.Key}", $",#${label.Value - address:X}");
                }
                else if (line.EndsWith($" {label.Key}"))
                {
                    Console.WriteLine($"Replacing {label.Key} with #${label.Value - address:X} ({label.Value:X} - {address:X})");
                    line = line.Replace($" {label.Key}", $" #${label.Value - address:X}");
                }
                else if (line.Contains($" {label.Key},"))
                {
                    Console.WriteLine($"Replacing {label.Key} with #${label.Value - address:X} ({label.Value:X} - {address:X})");
                    line = line.Replace($" {label.Key},", $" #${label.Value - address:X},");
                }
            }
            lines[index] = line;

            EndOfLoop:
            if (!IsLineEmpty(line))
            {
                ++address;
            }
        }
        
        return true;
    }

    public static void SetSimulatorMemory(ushort instruction)
    {
        _simulator.Memory[_simulator.PC++] = instruction;
    }

    public static bool ParseInstruction(string line)
    {
#if NET5_0_OR_GREATER
        var tokens = line.Trim().Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
#else
        var tokens = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        var destIndex = 0;
        for (int i = 0; i < tokens.Length; i++)
        {
            tokens[destIndex++] = tokens[i].Trim();

            if (tokens[destIndex - 1] == "")
            {
                destIndex--;
            }
        }
#endif
        
        if (tokens.Length == 0)
        {
            RegisterError("No Tokens");
            return false;
        }
        if (IsPseudoInstruction(tokens[0]))
        {
            try
            {
                var inst = PseudoInstructionParserTable[tokens[0]](tokens.SelectMany(token => token.Split(','))
                    .ToArray());
                for (var i = 0; i < inst.nWords; i++)
                {
                    SetSimulatorMemory(inst.words is null ? (ushort)0 : inst.words[i].Instruction);
                }
            }
            catch (Exception e)
            {
                RegisterError(e.Message);
                return false;
            }

            return true;
        }
        if (IsLC3Instruction(tokens[0]))
        {
            try
            {
                var instruction = InstructionParserTable[tokens[0]](tokens.SelectMany(token => token.Split(',')).ToArray());
                SetSimulatorMemory(instruction.Instruction);
            }
            catch (Exception e)
            {
                RegisterError(e.Message);
                return false;
            }
            return true;
        }
        RegisterError("Invalid instruction");
        return false;
    }

    #region Pseudo instruction Parsers
    
    public static (int, LC3Instruction[]?) ParseOrigin(string[] tokens)
    {
        if (tokens.Length != 2)
        {
            RegisterError("Invalid org instruction");
        }
        _simulator.PC = ParseNumber(tokens[1], 16);
        return (0, null);
    }
    
    public static (int, LC3Instruction[]?) ParseFill(string[] tokens)
    {
        if (tokens.Length != 2)
        {
            RegisterError("Invalid fill instruction");
        }
        return (1, [new LC3Instruction(ParseNumber(tokens[1], 16))]);
    }
    
    public static (int, LC3Instruction[]?) ParseData(string[] tokens)
    {
        if (tokens.Length != 2)
        {
            RegisterError("Invalid data instruction");
        }
        return (ParseNumber($"#{tokens[1]}", 16), null);
    }
    
    public static (int, LC3Instruction[]?) ParseString(string[] tokens)
    {
        if (tokens.Length != 2)
        {
            RegisterError("Invalid string instruction");
        }
        return (tokens[1].Length + 1, tokens[1].Select(c => new LC3Instruction(c)).Append(new LC3Instruction(0)).ToArray());
    }
    
    public static (int, LC3Instruction[]?) ParseEnd(string[] tokens)
    {
        if (tokens.Length != 1)
        {
            RegisterError("Invalid end instruction");
        }
        return (0, null);
    }

    #endregion

    #region LC3 Parsers
    
    public static LC3Instruction ParseBr(string[] tokens)
    {
        ushort instruction = 0x0000;
        if (tokens.Length != 2)
        {
            RegisterError($"Invalid BR instruction: {tokens}");
            return new LC3Instruction(instruction);
        }
        if (tokens[0].Contains('N'))
        {
            instruction |= 0x0800;
        }
        if (tokens[0].Contains('Z'))
        {
            instruction |= 0x0400;
        }
        if (tokens[0].Contains('P'))
        {
            instruction |= 0x0200;
        }
        if (tokens[0][2..].Length != ((instruction & 0x0200) >> 9) + ((instruction & 0x0400) >> 10) + ((instruction & 0x0800) >> 11))
        {
            RegisterError($"Invalid BR instruction {tokens[0]}");
        }

        instruction |= ParseNumber(tokens[1], 9);
        return new LC3Instruction(instruction);
    }

    public static LC3Instruction ParseAdd(string[] tokens)
    {
        ushort instruction = 0x1000;
        if (tokens.Length != 4)
        {
            RegisterError("Invalid ADD instruction");
        }
        instruction |= (ushort)(GetRegisterCode(tokens[1]) << 9);
        instruction |= (ushort)(GetRegisterCode(tokens[2]) << 6);
        if (tokens[3][0] == '#')
        {
            instruction |= 1 << 5;
            instruction |= ParseNumber(tokens[3], 5);
        }
        else
        {
            instruction |= GetRegisterCode(tokens[3]);
        }
        return new LC3Instruction(instruction);
    }
    
    public static LC3Instruction ParseLd(string[] tokens)
    {
        ushort instruction = 0x2000;
        if (tokens.Length != 3)
        {
            RegisterError("Invalid LD instruction");
        }
        instruction |= (ushort)(GetRegisterCode(tokens[1]) << 9);
        instruction |= ParseNumber(tokens[2], 9);
        return new LC3Instruction(instruction);
    }
    
    public static LC3Instruction ParseSt(string[] tokens)
    {
        ushort instruction = 0x3000;
        if (tokens.Length != 3)
        {
            RegisterError("Invalid ST instruction");
        }
        instruction |= (ushort)(GetRegisterCode(tokens[1]) << 9);
        instruction |= ParseNumber(tokens[2], 9);
        return new LC3Instruction(instruction);
    }
    
    public static LC3Instruction ParseJsr(string[] tokens)
    {
        ushort instruction = 0x4800;
        if (tokens.Length != 2)
        {
            RegisterError("Invalid JSR instruction");
        }
        instruction |= ParseNumber(tokens[1], 11);
        return new LC3Instruction(instruction);
    }
    
    public static LC3Instruction ParseJsrr(string[] tokens)
    {
        ushort instruction = 0x4000;
        if (tokens.Length != 2)
        {
            RegisterError("Invalid JSRR instruction");
        }
        instruction |= (ushort)(GetRegisterCode(tokens[1]) << 6);
        return new LC3Instruction(instruction);
    }
    
    public static LC3Instruction ParseAnd(string[] tokens)
    {
        ushort instruction = 0x5000;
        if (tokens.Length != 4)
        {
            RegisterError("Invalid AND instruction");
        }
        instruction |= (ushort)(GetRegisterCode(tokens[1]) << 9);
        instruction |= (ushort)(GetRegisterCode(tokens[2]) << 6);
        if (tokens[3][0] == '#')
        {
            instruction |= 1 << 5;
            instruction |= ParseNumber(tokens[3], 5);
        }
        else
        {
            instruction |= GetRegisterCode(tokens[3]);
        }
        return new LC3Instruction(instruction);
    }
    
    public static LC3Instruction ParseLdr(string[] tokens)
    {
        ushort instruction = 0x6000;
        if (tokens.Length != 4)
        {
            RegisterError("Invalid LDR instruction");
        }
        instruction |= (ushort)(GetRegisterCode(tokens[1]) << 9);
        instruction |= (ushort)(GetRegisterCode(tokens[2]) << 6);
        instruction |= ParseNumber(tokens[3], 6);
        return new LC3Instruction(instruction);
    }
    
    public static LC3Instruction ParseStr(string[] tokens)
    {
        ushort instruction = 0x7000;
        if (tokens.Length != 4)
        {
            RegisterError("Invalid STR instruction");
        }
        instruction |= (ushort)(GetRegisterCode(tokens[1]) << 9);
        instruction |= (ushort)(GetRegisterCode(tokens[2]) << 6);
        instruction |= ParseNumber(tokens[3], 6);
        return new LC3Instruction(instruction);
    }
    
    public static LC3Instruction ParseRti(string[] tokens)
    {
        ushort instruction = 0x8000;
        if (tokens.Length != 1)
        {
            RegisterError("Invalid RTI instruction");
        }
        return new LC3Instruction(instruction);
    }
    
    public static LC3Instruction ParseNot(string[] tokens)
    {
        ushort instruction = 0x903F;
        if (tokens.Length != 3)
        {
            RegisterError("Invalid NOT instruction");
        }
        instruction |= (ushort)(GetRegisterCode(tokens[1]) << 9);
        instruction |= (ushort)(GetRegisterCode(tokens[2]) << 6);
        return new LC3Instruction(instruction);
    }
    
    public static LC3Instruction ParseLdi(string[] tokens)
    {
        ushort instruction = 0xA000;
        if (tokens.Length != 3)
        {
            RegisterError("Invalid LDI instruction");
        }
        instruction |= (ushort)(GetRegisterCode(tokens[1]) << 9);
        instruction |= ParseNumber(tokens[2], 9);
        return new LC3Instruction(instruction);
    }
    
    public static LC3Instruction ParseSti(string[] tokens)
    {
        ushort instruction = 0xB000;
        if (tokens.Length != 3)
        {
            RegisterError("Invalid STI instruction");
        }
        instruction |= (ushort)(GetRegisterCode(tokens[1]) << 9);
        instruction |= ParseNumber(tokens[2], 9);
        return new LC3Instruction(instruction);
    }
    
    public static LC3Instruction ParseJmp(string[] tokens)
    {
        ushort instruction = 0xC000;
        if (tokens.Length != 2)
        {
            RegisterError("Invalid JMP instruction");
        }
        instruction |= (ushort)(GetRegisterCode(tokens[1]) << 6);
        return new LC3Instruction(instruction);
    }
    
    public static LC3Instruction ParseRet(string[] tokens)
    {
        if (tokens.Length is not 1)
        {
            RegisterError("Invalid RET instruction");
        }
        return ParseJmp(["JMP", "R7"]);
    }
    
    public static LC3Instruction ParseLea(string[] tokens)
    {
        ushort instruction = 0xE000;
        if (tokens.Length != 3)
        {
            RegisterError("Invalid LEA instruction");
        }
        instruction |= (ushort)(GetRegisterCode(tokens[1]) << 9);
        instruction |= ParseNumber(tokens[2], 9);
        return new LC3Instruction(instruction);
    }
    
    public static LC3Instruction ParseTrap(string[] tokens)
    {
        ushort instruction = 0xF000;
        if (tokens.Length != 2)
        {
            RegisterError("Invalid TRAP instruction");
            return new LC3Instruction(instruction);
        }
        instruction |= ParseNumber($"#${tokens[1][1..]}", 8);
        return new LC3Instruction(instruction);
    }
    
    #endregion

    public static bool Parse(string[] lines)
    {
        var flag = true;
        for (_currentLineIndex = 0; _currentLineIndex < lines.Length; _currentLineIndex++)
        {
            _currentLine = lines[_currentLineIndex];
            if (_currentLine is "")
            {
                continue;
            }
            if (!ParseInstruction(_currentLine))
            {
                flag = false;
            }
        }

        return flag;
    }

    public static bool Compile(string inputFile, string outputFile)
    {
        var lines = File.ReadAllLines(inputFile);
        lines = StripComments(lines);
        if (!ParseLabels(lines))
        {
            RegisterError("Could not parse labels");
            return false;
        }
        lines = StripLabels(lines);
        //lines = RemoveEmptyLines(lines);
        if (!Parse(lines))
        {
            Console.WriteLine("Errors found in compilation");
            return false;
        }
        _simulator.WriteMemoryToFile(outputFile);
        return true;
    }

    public static bool Compile(string inputFile, [NotNullWhen(true)] out Simulator? simulator)
    {
        simulator = null;
        var lines = File.ReadAllLines(inputFile);
        lines = StripComments(lines);
        if (!ParseLabels(lines))
        {
            RegisterError("Could not parse labels");
            return false;
        }
        lines = StripLabels(lines);
        //lines = RemoveEmptyLines(lines);
        if (!Parse(lines))
        {
            Console.WriteLine("Errors found in compilation");
            return false;
        }
        simulator = _simulator;
        return true;
    }
}