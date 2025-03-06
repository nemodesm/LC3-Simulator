namespace LC3_Simulator;

public class Simulator
{
    // OPCode = 4 bits
    // In/Out registers = 3 bits
    public const uint MemorySize = 0x10000;
    public const byte WordSize = 16;
    public const byte OpCodeSize = 4;
    public const byte RegisterSize = 3;

    #region Registers

    public ushort R0;
    public ushort R1;
    public ushort R2;
    public ushort R3;
    public ushort R4;
    public ushort R5;
    public ushort R6;
    public ushort R7;

    public ushort PC;
    /// <summary>
    /// 0000_0NZP (N = negative, Z = zero, P = positive)
    /// </summary>
    public byte CC;

    #endregion
    
    public ushort[] Memory = new ushort[MemorySize];
    
    public Dictionary<byte, Action<LC3Instruction>> Instructions;
    
    public Simulator()
    {
        R0 = 0;
        R1 = 0;
        R2 = 0;
        R3 = 0;
        R4 = 0;
        R5 = 0;
        R6 = 0;
        R7 = 0;
        PC = 0;
        CC = 0;

        Instructions = new()
        {
            { 0b0000, BR  },
            { 0b0001, ADD },
            { 0b0010, LD  },
            { 0b0011, ST  },
            { 0b0100, JSR },
            { 0b0101, AND },
            { 0b0110, LDR },
            { 0b0111, STR },
            { 0b1000, (_) => throw new NotImplementedException() }, // RTI ?? unused? (ReTurn Interrupt)
            { 0b1001, NOT },
            { 0b1010, LDI },
            { 0b1011, STI },
            { 0b1100, JMP },
            { 0b1101, RET },
            { 0b1110, LEA },
            { 0b1111, TRAP }, // TRAP
        };
    }
    
    public void SetRegisterValue(byte register, ushort value)
    {
        SetCCFromValue(value);
        switch (register)
        {
            case 0: R0 = value; break;
            case 1: R1 = value; break;
            case 2: R2 = value; break;
            case 3: R3 = value; break;
            case 4: R4 = value; break;
            case 5: R5 = value; break;
            case 6: R6 = value; break;
            case 7: R7 = value; break;
            default: throw new IndexOutOfRangeException();
        }
    }

    public ushort GetRegisterValue(byte register) =>
        register switch
        {
            0 => R0,
            1 => R1,
            2 => R2,
            3 => R3,
            4 => R4,
            5 => R5,
            6 => R6,
            7 => R7,
            _ => throw new IndexOutOfRangeException()
        };

    public ushort GetMemoryFromPCOffset(short offset) => Memory[PC + offset];
    
    public ushort GetMemoryFromRegisterOffset(byte register, short offset) => Memory[GetRegisterValue(register) + offset];
    
    public void SetMemoryFromPCOffset(short offset, ushort value) => Memory[PC + offset] = value;
    
    public void SetMemoryFromRegisterOffset(byte register, short offset, ushort value) => Memory[GetRegisterValue(register) + offset] = value;

    /// <summary>
    /// Converts an unsigned 8-bit integer to a signed 8-bit integer by performing two's complement.
    /// </summary>
    /// <param name="unsigned">The unsigned 8-bit integer to convert.</param>
    /// <returns>The signed 8-bit integer equivalent to the given unsigned 8-bit integer.</returns>
    public sbyte UnsignedToSigned(byte unsigned) => unsigned > 0x7F ? (sbyte)(~unsigned + 1) : (sbyte)unsigned;
    
    /// <summary>
    /// Converts an unsigned 16-bit integer to a signed 16-bit integer by performing two's complement.
    /// </summary>
    /// <param name="unsigned">The unsigned 16-bit integer to convert.</param>
    /// <returns>The signed 16-bit integer equivalent to the given unsigned 16-bit integer.</returns>
    public short UnsignedToSigned(ushort unsigned) => unsigned > 0x7FFF ? (short)(~unsigned + 1) : (short)unsigned;

    /// <summary>
    /// Converts an unsigned n-bit integer to a signed n-bit integer by performing two's complement.
    /// </summary>
    /// <param name="unsigned">The unsigned 16-bit integer to convert.</param>
    /// <param name="bits">The number of bits in the integer.</param>
    /// <returns>The signed n-bit integer equivalent to the given unsigned n-bit integer.</returns>
    public short UnsignedToSigned(ushort unsigned, byte bits)
    {
        var mask = (ushort)(1 << (bits - 1));
        if ((unsigned & mask) != 0)
        {
            return (short)-((~unsigned + 1) & ((1 << bits) - 1));
        }
        return (short)unsigned;
    }

    public void SetCCFromValue(ushort value)
    {
        if (value == 0)
        {
            CC = 0b010;
        }
        else if ((value & 0x10_00) != 0)
        {
            CC = 0b100;
        }
        else
        {
            CC = 0b001;
        }
    }

    public void ExecuteInstruction(LC3Instruction instruction)
    {
        var opCode = instruction.GetOpCode();
        var instructionMethod = Instructions[opCode];
        instructionMethod(instruction);
    }
    
    public void Execute()
    {
        while (true)
        {
#if DEBUG
            Console.WriteLine((Memory[PC] >> 12) switch
            {
                0b0000 => "BR",
                0b0001 => "ADD",
                0b0010 => "LD",
                0b0011 => "ST",
                0b0100 => "JSR",
                0b0101 => "AND",
                0b0110 => "LDR",
                0b0111 => "STR",
                0b1000 => "RTI",
                0b1001 => "NOT",
                0b1010 => "LDI",
                0b1011 => "STI",
                0b1100 => "JMP",
                0b1101 => "RET",
                0b1110 => "LEA",
                0b1111 => "TRAP",
                _ => throw new ArgumentOutOfRangeException()
            });
#endif
            var ppc = PC++;
            ExecuteInstruction(new LC3Instruction(Memory[ppc]));
        }
    }
    
    public void LoadFromFile(string inputFile)
    {
        var file = File.OpenRead(inputFile);
        var reader = new BinaryReader(file);
        for (var index = 0; index < Memory.Length; index++)
        {
            Memory[index] = reader.ReadUInt16();
            
            // swap bytes
            Memory[index] = (ushort)((Memory[index] << 8) | (Memory[index] >> 8));
        }
    }

    #region OpCode Impl

    public void NOT(LC3Instruction instruction)
    {
        var dest = instruction.GetDestRegister();
        var src = instruction.GetSrcRegister();
        SetRegisterValue(dest, (ushort)~GetRegisterValue(src));
    }

    public void ADD(LC3Instruction instruction)
    {
        var dest = instruction.GetDestRegister();
        var src1 = UnsignedToSigned(GetRegisterValue(instruction.GetSrcRegister()));
        short src2;
        bool registerMode = instruction.GetBit(5) == 0;

        if (registerMode)
        {
            src2 = UnsignedToSigned(GetRegisterValue((byte)instruction.GetBits(0, 3)));
        }
        else
        {
            src2 = UnsignedToSigned(instruction.GetBits(0, 5), 5); // TODO: this is immediate but needs to be sign extended
        }

        var sum = (ushort)(src1 + src2);
        SetRegisterValue(dest, sum);
    }

    public void AND(LC3Instruction instruction)
    {
        var dest = instruction.GetDestRegister();
        var src1 = UnsignedToSigned(GetRegisterValue(instruction.GetSrcRegister()));
        ushort src2;
        bool registerMode = instruction.GetBit(5) == 0;

        if (registerMode)
        {
            src2 = GetRegisterValue((byte)instruction.GetBits(0, 3));
        }
        else
        {
            src2 = instruction.GetBits(0, 5);
        }

        var sum = (ushort)(src1 & src2);
        SetRegisterValue(dest, sum);
    }
    
    public void LD(LC3Instruction instruction)
    {
        var dest = instruction.GetDestRegister();
        var offset = instruction.GetBits(0, 9);
        var loadedValue = GetMemoryFromPCOffset(UnsignedToSigned(offset, 9));
        SetRegisterValue(dest, loadedValue);
    }
    
    public void ST(LC3Instruction instruction)
    {
        var src = instruction.GetDestRegister();
        var offset = instruction.GetBits(0, 9);
        SetMemoryFromPCOffset(UnsignedToSigned(offset, 9), GetRegisterValue(src));
    }
    
    public void LDI(LC3Instruction instruction)
    {
        var dest = instruction.GetDestRegister();
        var offset = instruction.GetBits(0, 9);
        var loadedAddress = GetMemoryFromPCOffset(UnsignedToSigned(offset, 9));
        SetRegisterValue(dest, Memory[loadedAddress]);
    }
    
    public void STI(LC3Instruction instruction)
    {
        var src = instruction.GetDestRegister();
        var offset = instruction.GetBits(0, 9);
        var loadedAddress = GetMemoryFromPCOffset(UnsignedToSigned(offset, 9));
        Memory[loadedAddress] = GetRegisterValue(src);
    }
    
    public void LDR(LC3Instruction instruction)
    {
        var dest = instruction.GetDestRegister();
        var baseRegister = instruction.GetSrcRegister();
        var offset = instruction.GetBits(0, 6);
        var loadedValue = GetMemoryFromRegisterOffset(baseRegister, UnsignedToSigned(offset, 6));
        SetRegisterValue(dest, loadedValue);
    }
    
    public void STR(LC3Instruction instruction)
    {
        var src = instruction.GetDestRegister();
        var baseRegister = instruction.GetSrcRegister();
        var offset = instruction.GetBits(0, 6);
        SetMemoryFromRegisterOffset(baseRegister, UnsignedToSigned(offset, 6), GetRegisterValue(src));
    }

    public void LEA(LC3Instruction instruction)
    {
        var dest = instruction.GetDestRegister();
        var offset = instruction.GetBits(0, 9);
        var loadedValue = (ushort)(PC + UnsignedToSigned(offset, 9));
        SetRegisterValue(dest, loadedValue);
    }
    
    public void BR(LC3Instruction instruction)
    {
        var offset = instruction.GetBits(0, 9);
        var breakNegative = instruction.GetBit(11) == 1;
        if (breakNegative && (CC & 4) != 0)
        {
            PC = (ushort)(PC + UnsignedToSigned(offset, 9));
            return;
        }
        var breakZero = instruction.GetBit(10) == 1;
        if (breakZero && (CC & 2) != 0)
        {
            PC = (ushort)(PC + UnsignedToSigned(offset, 9));
            return;
        }
        var breakPos = instruction.GetBit(9) == 1;
        if (breakPos && (CC & 1) != 0)
        {
            PC = (ushort)(PC + UnsignedToSigned(offset, 9));
            return;
        }
    }
    
    public void JMP(LC3Instruction instruction)
    {
        var baseRegister = instruction.GetSrcRegister();
        PC = GetRegisterValue(baseRegister);
    }

    public void JSR(LC3Instruction instruction)
    {
        var isJSRR = instruction.GetBit(11) == 0;
        if (isJSRR)
        {
            var src = instruction.GetSrcRegister();
            R7 = PC;
            PC = GetRegisterValue(src);
        }
        else
        {
            var offset = instruction.GetBits(0, 11);
            var signedOffset = UnsignedToSigned(offset, 11);
            R7 = PC;
            PC = (ushort)(PC + signedOffset);
        }
    }
    
    public void RET(LC3Instruction instruction)
    {
        PC = R7;
    }
    
    public void TRAP(LC3Instruction instruction)
    {
        Console.WriteLine("TRAP reached, dumping memory");
        // TODO: actual implementation
        // for now, dumps memory to mem.dmp file and terminates
        using (var writer = File.CreateText("mem.dmp"))
        {
            writer.WriteLine("LC-3 Memory Dump");
            writer.WriteLine($"=== {DateTime.Now} ===");
            writer.WriteLine();
            writer.WriteLine($"Registers:");
            writer.WriteLine($"PC: {PC}");
            writer.WriteLine($"CC: {CC}");
            writer.WriteLine($"R0: {R0}");
            writer.WriteLine($"R1: {R1}");
            writer.WriteLine($"R2: {R2}");
            writer.WriteLine($"R3: {R3}");
            writer.WriteLine($"R4: {R4}");
            writer.WriteLine($"R5: {R5}");
            writer.WriteLine($"R6: {R6}");
            writer.WriteLine($"R7: {R7}");
            writer.WriteLine();
            writer.WriteLine("Memory:");
            for (var index = 0; index < Memory.Length; index+= 4)
            {
                writer.Write(Memory[index].ToString("b16"));
                writer.Write(" ");
                writer.Write(Memory[index + 1].ToString("b16"));
                writer.Write(" ");
                writer.Write(Memory[index + 2].ToString("b16"));
                writer.Write(" ");
                writer.Write(Memory[index + 3].ToString("b16"));
                writer.Write(" -- ");
                // rewrite in hex
                writer.Write(Memory[index].ToString("x4"));
                writer.Write(" ");
                writer.Write(Memory[index + 1].ToString("x4"));
                writer.Write(" ");
                writer.Write(Memory[index + 2].ToString("x4"));
                writer.Write(" ");
                writer.Write(Memory[index + 3].ToString("x4"));
                writer.WriteLine();
            }
        }
        Environment.Exit(0);
    }

    #endregion
}