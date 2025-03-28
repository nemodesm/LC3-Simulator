using System.Diagnostics.CodeAnalysis;

namespace LC3_Simulator;

public static class CompilerV1
{
    private static int _currentLineIndex = 0;
    private static string _currentLine = "";
    public static void RegisterWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Warning: {message}");
        if (_currentLine != "")
        {
            Console.WriteLine($"Near line {_currentLineIndex + 1} --> {_currentLine.Trim()}");
        }
        Console.ResetColor();
    }

    public static byte GetRegisterCode(string register)
    {
        return (byte)(register[1] - '0');
    }
    
    public static ushort ParseNumber(string number, byte bits)
    {
        var mask = (1 << bits) - 1;
        int parsed;
        try
        {
            parsed = number[1] switch
            {
                '$' => int.Parse(number[2..], System.Globalization.NumberStyles.HexNumber),
                'b' => int.Parse(number[2..], System.Globalization.NumberStyles.BinaryNumber),
                _ => int.Parse(number[1..])
            };
        }
        catch (FormatException e)
        {
            throw new FormatException($"Could not parse number {number}: Invalid Format", e);
        }
        if (parsed > mask || parsed < 0)
        {
            RegisterWarning($"Number {number} (parsed to {(uint)parsed}) is too large for {bits} bits, this can cause unexpected behavior");
        }
        return (ushort)(parsed & mask);
    }
    
    public static bool ParseInstruction(string line, [NotNullWhen(true)] out ushort? instruction)
    {
        ushort outInstruction = 0;
        instruction = null;
        if (line.Contains(';'))
        {
            line = line[..line.IndexOf(';')];
        }
        var elements = line.Trim().Split(' ');
        if (elements.Length == 0 || elements[0].Length == 0)
        {
            return false;
        }

        byte dest;
        byte src1;
        byte src2;
        byte lastReadIndex = 0;
        switch (elements[0])
        {
            case "BRN":
                outInstruction = 0b0000_1_0_0_000000000;
                outInstruction |= ParseNumber(elements[1], 9);
                lastReadIndex = 1;
                break;
            case "BRZ":
                outInstruction = 0b0000_0_1_0_000000000;
                outInstruction |= ParseNumber(elements[1], 9);
                lastReadIndex = 1;
                break;
            case "BRP":
                outInstruction = 0b0000_0_0_1_000000000;
                outInstruction |= ParseNumber(elements[1], 9);
                lastReadIndex = 1;
                break;
            case "ADD":
                outInstruction = 0b0001_000_000_0_00000;
                dest = GetRegisterCode(elements[1]);
                src1 = GetRegisterCode(elements[2]);
                outInstruction |= (ushort)(dest << 9);
                outInstruction |= (ushort)(src1 << 6);
                if(elements[3][0] == 'R')
                {
                    src2 = GetRegisterCode(elements[3]);
                    outInstruction |= src2;
                }
                else if (elements[3][0] == '#')
                {
                    outInstruction |= ParseNumber(elements[3], 5);
                    outInstruction |= 0b100000;
                }
                else
                {
                    throw new Exception($"Invalid operand: {elements[3]}");
                }
                lastReadIndex = 3;
                break;
            case "LD":
                outInstruction = 0b0010_000_000_000000;
                dest = GetRegisterCode(elements[1]);
                var offset = ParseNumber(elements[2], 9);
                outInstruction |= (ushort)(dest << 9);
                outInstruction |= offset;
                lastReadIndex = 2;
                break;
            case "ST":
                outInstruction = 0b0011_000_000_000000;
                src1 = GetRegisterCode(elements[1]);
                offset = ParseNumber(elements[2], 9);
                outInstruction |= (ushort)(src1 << 9);
                outInstruction |= offset;
                lastReadIndex = 2;
                break;
            case "JSR":
                outInstruction = 0b0100_1_00000000000;
                offset = ParseNumber(elements[1], 11);
                outInstruction |= offset;
                lastReadIndex = 1;
                break;
            case "JSRR":
                outInstruction = 0b0100_0_00_000_000000;
                dest = GetRegisterCode(elements[1]);
                outInstruction |= (ushort)(dest << 6);
                lastReadIndex = 1;
                break;
            case "AND":
                outInstruction = 0b0101_000_000_000000;
                dest = GetRegisterCode(elements[1]);
                src1 = GetRegisterCode(elements[2]);
                outInstruction |= (ushort)(dest << 9);
                outInstruction |= (ushort)(src1 << 6);
                if(elements[3][0] == 'R')
                {
                    src2 = GetRegisterCode(elements[3]);
                    outInstruction |= src2;
                }
                else if (elements[3][0] == '#')
                {
                    outInstruction |= ParseNumber(elements[3], 5);
                }
                else
                {
                    throw new Exception($"Invalid operand: {elements[3]}");
                }
                lastReadIndex = 3;
                break;
            case "LDR":
                outInstruction = 0b0110_000_000_000000;
                dest = GetRegisterCode(elements[1]);
                var baseRegister = GetRegisterCode(elements[2]);
                offset = ParseNumber(elements[3], 6);
                
                outInstruction |= (ushort)(dest << 9);
                outInstruction |= (ushort)(baseRegister << 6);
                outInstruction |= offset;
                lastReadIndex = 3;
                break;
            case "STR":
                outInstruction = 0b0111_000_000_000000;
                src1 = GetRegisterCode(elements[1]);
                baseRegister = GetRegisterCode(elements[2]);
                offset = ParseNumber(elements[3], 6);
                outInstruction |= (ushort)(src1 << 9);
                outInstruction |= (ushort)(baseRegister << 6);
                outInstruction |= offset;
                lastReadIndex = 3;
                break;
            case "RTI":
                throw new NotImplementedException();
            case "NOT":
                outInstruction = 0b1001_000_000_000000;
                dest = GetRegisterCode(elements[1]);
                src1 = GetRegisterCode(elements[2]);
                outInstruction |= (ushort)(dest << 9);
                outInstruction |= (ushort)(src1 << 6);
                lastReadIndex = 2;
                break;
            case "LDI":
                outInstruction = 0b1010_000_000_000000;
                dest = GetRegisterCode(elements[1]);
                offset = ParseNumber(elements[2], 9);
                outInstruction |= (ushort)(dest << 9);
                outInstruction |= offset;
                lastReadIndex = 2;
                break;
            case "STI":
                outInstruction = 0b1011_000_000_000000;
                src1 = GetRegisterCode(elements[1]);
                offset = ParseNumber(elements[2], 9);
                outInstruction |= (ushort)(src1 << 9);
                outInstruction |= offset;
                lastReadIndex = 2;
                break;
            case "JMP":
                outInstruction = 0b1100_000_000_000000;
                dest = GetRegisterCode(elements[1]);
                outInstruction |= (ushort)(dest << 6);
                lastReadIndex = 1;
                break;
            case "RET":
                outInstruction = 0b1101_000000000000;
                break;
            case "LEA":
                outInstruction = 0b1110_000_000_000000;
                dest = GetRegisterCode(elements[1]);
                offset = ParseNumber(elements[2], 9);
                outInstruction |= (ushort)(dest << 9);
                outInstruction |= offset;
                lastReadIndex = 2;
                break;
            case "TRAP":
                outInstruction = 0b1111_000_000_000000;
                // TODO: better trap handling
                break;
            case "loc":
                outInstruction = ParseNumber(elements[1], 16);
                lastReadIndex = 1;
                break;
            default:
                if (elements[0].StartsWith(';'))
                {
                    return false;
                }
                throw new Exception("Invalid instruction");
        }

        if (lastReadIndex < elements.Length - 1 && !elements[lastReadIndex + 1].StartsWith(';'))
        {
            throw new Exception("Invalid instruction");
        }

        instruction = outInstruction;
        return elements[0] != "loc";
    }

    public static bool ParseProgram(string inputFile, [NotNullWhen(true)] out LC3Instruction[]? instructions)
    {
        // TODO: Add support for data blocks
        
        var tmpInstructions = new LC3Instruction?[Simulator.MemorySize];
        var instructionIndex = 0;
        var lines = File.ReadAllLines(inputFile);
        var hasErrors = false;
        for (_currentLineIndex = 0; _currentLineIndex < lines.Length; _currentLineIndex++)
        {
            _currentLine = lines[_currentLineIndex];
            try
            {
                if (ParseInstruction(_currentLine, out var instruction))
                {
                    tmpInstructions[instructionIndex++] = new LC3Instruction(instruction.Value);
                }
                else if (instruction != null) // instruction was `loc [address]`
                {
                    instructionIndex = instruction.Value;
                }
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"""
                                         Error: {e.Message}
                                         Near line {_currentLineIndex + 1} --> {_currentLine.Trim()}
                                         """);
                Console.ResetColor();
                hasErrors = true;
            }
        }
        
        if (hasErrors)
        {
            Console.Error.WriteLine("""
                                    Could not compile program
                                    Please fix the above errors and try again
                                    """);
            instructions = null;
            return false;
        }

        instructions = new LC3Instruction[Simulator.MemorySize];
        instructionIndex = 0;
        foreach (var instruction in tmpInstructions)
        {
            if (instruction != null)
            {
                instructions[instructionIndex++] = instruction;
            }
            else
            {
                instructions[instructionIndex++] = new LC3Instruction(0);
            }
        }

        return true;
    }
    
    public static bool Compile(string inputFile, string outputFile)
    {
        var file = File.Create(outputFile);
        var writer = new BinaryWriter(file);
        if (ParseProgram(inputFile, out var instructions))
        {
            foreach (var instruction in instructions)
            {
                var writtenInstruction = (ushort)((instruction.Instruction & 0x00FF) << 8);
                writtenInstruction |= (ushort)((instruction.Instruction & 0xFF00) >> 8);
                writer.Write(writtenInstruction);
            }
            file.Close();
            return true;
        }
        file.Close();
        File.Delete(outputFile);
        return false;
    }
}