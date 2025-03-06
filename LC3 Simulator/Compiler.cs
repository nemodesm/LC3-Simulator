using System.Diagnostics.CodeAnalysis;

namespace LC3_Simulator;

public static class Compiler
{
    public static byte GetRegisterCode(string register)
    {
        return (byte)(register[1] - '0');
    }
    
    public static ushort ParseNumber(string number, byte bits)
    {
        var mask = (1 << bits) - 1;
        return (ushort)(number[1] switch
        {
            '$' => int.Parse(number[2..], System.Globalization.NumberStyles.HexNumber) & mask,
            'b' => int.Parse(number[2..], System.Globalization.NumberStyles.BinaryNumber) & mask,
            _ => int.Parse(number[1..]) & mask
        });
    }
    
    public static bool ParseInstruction(string line, [NotNullWhen(true)] out ushort? instruction)
    {
        ushort outInstruction = 0;
        instruction = null;
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
                instruction = 0b0000_1_0_0_000000000;
                instruction |= ParseNumber(elements[1], 9);
                lastReadIndex = 1;
                break;
            case "BRZ":
                instruction = 0b0000_0_1_0_000000000;
                instruction |= ParseNumber(elements[1], 9);
                lastReadIndex = 1;
                break;
            case "BRP":
                instruction = 0b0000_0_0_1_000000000;
                instruction |= ParseNumber(elements[1], 9);
                lastReadIndex = 1;
                break;
            case "ADD":
                instruction = 0b0001_000_000_0_00000;
                dest = GetRegisterCode(elements[1]);
                src1 = GetRegisterCode(elements[2]);
                instruction |= (ushort)(dest << 9);
                instruction |= (ushort)(src1 << 6);
                if(elements[3][0] == 'R')
                {
                    src2 = GetRegisterCode(elements[3]);
                    instruction |= src2;
                }
                else if (elements[3][0] == '#')
                {
                    instruction |= ParseNumber(elements[3], 5);
                    instruction |= 0b100000;
                }
                else
                {
                    throw new Exception($"Invalid operand: {elements[3]}");
                }
                lastReadIndex = 3;
                break;
            case "LD":
                instruction = 0b0010_000_000_000000;
                dest = GetRegisterCode(elements[1]);
                var offset = ParseNumber(elements[2], 9);
                instruction |= (ushort)(dest << 9);
                instruction |= offset;
                lastReadIndex = 2;
                break;
            case "ST":
                instruction = 0b0011_000_000_000000;
                src1 = GetRegisterCode(elements[1]);
                offset = ParseNumber(elements[2], 9);
                instruction |= (ushort)(src1 << 9);
                instruction |= offset;
                lastReadIndex = 2;
                break;
            case "JSR":
                instruction = 0b0100_1_00000000000;
                offset = ParseNumber(elements[1], 11);
                instruction |= offset;
                lastReadIndex = 1;
                break;
            case "JSRR":
                instruction = 0b0100_0_00_000_000000;
                dest = GetRegisterCode(elements[1]);
                instruction |= (ushort)(dest << 6);
                lastReadIndex = 1;
                break;
            case "AND":
                instruction = 0b0101_000_000_000000;
                dest = GetRegisterCode(elements[1]);
                src1 = GetRegisterCode(elements[2]);
                instruction |= (ushort)(dest << 9);
                instruction |= (ushort)(src1 << 6);
                if(elements[3][0] == 'R')
                {
                    src2 = GetRegisterCode(elements[3]);
                    instruction |= src2;
                }
                else if (elements[3][0] == '#')
                {
                    instruction |= ParseNumber(elements[3], 5);
                }
                else
                {
                    throw new Exception($"Invalid operand: {elements[3]}");
                }
                lastReadIndex = 3;
                break;
            case "LDR":
                instruction = 0b0110_000_000_000000;
                dest = GetRegisterCode(elements[1]);
                var baseRegister = GetRegisterCode(elements[2]);
                offset = ParseNumber(elements[3], 6);
                
                instruction |= (ushort)(dest << 9);
                instruction |= (ushort)(baseRegister << 6);
                instruction |= offset;
                lastReadIndex = 3;
                break;
            case "STR":
                instruction = 0b0111_000_000_000000;
                src1 = GetRegisterCode(elements[1]);
                baseRegister = GetRegisterCode(elements[2]);
                offset = ParseNumber(elements[3], 6);
                instruction |= (ushort)(src1 << 9);
                instruction |= (ushort)(baseRegister << 6);
                instruction |= offset;
                lastReadIndex = 3;
                break;
            case "RTI":
                throw new NotImplementedException();
            case "NOT":
                instruction = 0b1001_000_000_000000;
                dest = GetRegisterCode(elements[1]);
                src1 = GetRegisterCode(elements[2]);
                instruction |= (ushort)(dest << 9);
                instruction |= (ushort)(src1 << 6);
                lastReadIndex = 2;
                break;
            case "LDI":
                instruction = 0b1010_000_000_000000;
                dest = GetRegisterCode(elements[1]);
                offset = ParseNumber(elements[2], 9);
                instruction |= (ushort)(dest << 9);
                instruction |= offset;
                lastReadIndex = 2;
                break;
            case "STI":
                instruction = 0b1011_000_000_000000;
                src1 = GetRegisterCode(elements[1]);
                offset = ParseNumber(elements[2], 9);
                instruction |= (ushort)(src1 << 9);
                instruction |= offset;
                lastReadIndex = 2;
                break;
            case "JMP":
                instruction = 0b1100_000_000_000000;
                dest = GetRegisterCode(elements[1]);
                instruction |= (ushort)(dest << 6);
                lastReadIndex = 1;
                break;
            case "RET":
                instruction = 0b1101_000000000000;
                break;
            case "LEA":
                instruction = 0b1110_000_000_000000;
                dest = GetRegisterCode(elements[1]);
                offset = ParseNumber(elements[2], 9);
                instruction |= (ushort)(dest << 9);
                instruction |= offset;
                lastReadIndex = 2;
                break;
            case "TRAP":
                instruction = 0b1111_000_000_000000;
                // TODO: better trap handling
                break;
            case "loc":
                instruction = ParseNumber(elements[1], 16);
                lastReadIndex = 1;
                break;
            default:
                if (elements[0].StartsWith(';'))
                {
                    return false;
                }
                throw new Exception("Invalid instruction");
        }

        if (lastReadIndex < elements.Length - 1)
        {
            if (elements[lastReadIndex + 1].StartsWith(';'))
            {
                return instruction != null && elements[0] != "loc";
            }
            throw new Exception("Invalid instruction");
        }
        return instruction != null && elements[0] != "loc";
    }

    public static bool ParseProgram(string inputFile, [NotNullWhen(true)] out LC3Instruction[]? instructions)
    {
        // TODO: Add support for data blocks
        
        var tmpInstructions = new LC3Instruction?[Simulator.MemorySize];
        var instructionIndex = 0;
        var lines = File.ReadAllLines(inputFile);
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            try
            {
                if (ParseInstruction(line, out var instruction))
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
                Console.Error.WriteLine($"""
                                         Error: Could not compile program
                                         Near line {index + 1} --> {line}

                                         {e.Message}
                                         """);
                instructions = null;
                return false;
            }
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