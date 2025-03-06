namespace LC3_Simulator;

public class LC3Instruction
{
    public const byte WordSize = Simulator.WordSize;
    public const byte OpCodeSize = Simulator.OpCodeSize;
    public const byte RegisterSize = Simulator.RegisterSize;
    
    public ushort Instruction { get; }

    public LC3Instruction(ushort instruction)
    {
        Instruction = instruction;
    }

    public byte GetOpCode()
    {
        return (byte)(Instruction >> (WordSize - OpCodeSize));
    }
    
    public byte GetDestRegister()
    {
        return (byte)((Instruction >> (WordSize - OpCodeSize - RegisterSize)) & 7);
    }
    
    public byte GetSrcRegister()
    {
        return (byte)((Instruction >> (WordSize - OpCodeSize - RegisterSize * 2)) & 7);
    }
    
    public ushort GetBits(byte start, byte length)
    {
        return (ushort)((Instruction >> start) & ((1 << length) - 1));
    }
    
    public byte GetBit(byte index)
    {
        return (byte)((Instruction >> index) & 1);
    }
}