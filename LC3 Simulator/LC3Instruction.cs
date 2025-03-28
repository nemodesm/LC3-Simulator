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
    
    protected bool Equals(LC3Instruction? other) => Instruction == other?.Instruction;

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((LC3Instruction)obj);
    }

    public override int GetHashCode() => Instruction.GetHashCode();
    
    public static bool operator ==(LC3Instruction? left, LC3Instruction? right)
    {
        return left?.Equals(right) ?? right is null;
    }

    public static bool operator !=(LC3Instruction? left, LC3Instruction? right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return $"{Instruction:X4}";
    }
}