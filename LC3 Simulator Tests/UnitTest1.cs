using LC3_Simulator;

namespace LC3_Simulator_Tests;

public class CompilerTests
{
    [SetUp]
    public void Setup()
    {
    }

    #region Parse Numbers
    
    [Test]
    public void ParseNumberDecimal()
    {
        Assert.That(Compiler.ParseNumber("#0000", 16), Is.EqualTo(0));
        Assert.That(Compiler.ParseNumber("#-1", 16), Is.EqualTo(0xFFFF));
        Assert.That(Compiler.ParseNumber("#0010", 16), Is.EqualTo(10));
        Assert.That(Compiler.ParseNumber("#0011", 2), Is.EqualTo(3));
    }
    
    [Test]
    public void ParseNumberHex()
    {
        Assert.That(Compiler.ParseNumber("#$0000", 2), Is.EqualTo((ushort)0));
        Assert.That(Compiler.ParseNumber("#$FF00", 2), Is.EqualTo((ushort)0));
        Assert.That(Compiler.ParseNumber("#$FF00", 16), Is.EqualTo((ushort)0xFF00));
        Assert.That(Compiler.ParseNumber("#$FF00", 16), Is.EqualTo((ushort)0xFF00));
    }
    
    [Test]
    public void ParseNumberBinary()
    {
        Assert.That(Compiler.ParseNumber("#b0000", 2), Is.EqualTo(0));
        Assert.That(Compiler.ParseNumber("#b10000001", 9), Is.EqualTo(129));
        Assert.That(Compiler.ParseNumber("#b10000001", 8), Is.EqualTo(129));
        Assert.That(Compiler.ParseNumber("#b10000001", 6), Is.EqualTo(1));
    }
    
    [Test]
    public void ParseNumberInvalid()
    {
        Assert.Throws<FormatException>(() => _ = Compiler.ParseNumber("#bFF00", 2));
        Assert.Throws<FormatException>(() => _ = Compiler.ParseNumber("#$0xFF00", 16));
        Assert.Throws<FormatException>(() => _ = Compiler.ParseNumber("#FF00", 16));
        Assert.Throws<FormatException>(() => _ = Compiler.ParseNumber("#b-10001001", 16));
    }

    #endregion

    #region Label Valid Tests

    [Test]
    public void LabelValidTests()
    {
        Assert.That(Compiler.IsLabelValid("R0"), Is.False);
        Assert.That(Compiler.IsLabelValid("R1"), Is.False);
        Assert.That(Compiler.IsLabelValid("R2"), Is.False);
        Assert.That(Compiler.IsLabelValid("R3"), Is.False);
        Assert.That(Compiler.IsLabelValid("R4"), Is.False);
        Assert.That(Compiler.IsLabelValid("R5"), Is.False);
        Assert.That(Compiler.IsLabelValid("R6"), Is.False);
        Assert.That(Compiler.IsLabelValid("R7"), Is.False);
        Assert.That(Compiler.IsLabelValid("R8"), Is.True);
        Assert.That(Compiler.IsLabelValid("\\R6"), Is.True);
        Assert.That(Compiler.IsLabelValid("Main\\Label"), Is.False);
        Assert.That(Compiler.IsLabelValid("Hello I am a label"), Is.False);
        Assert.That(Compiler.IsLabelValid("HelloIamalabel"), Is.True);
    }

    #endregion
    
    #region Line Empty Tests

    [Test]
    public void LineEmpty()
    {
        Assert.That(Compiler.IsLineEmpty(""), Is.True);
        Assert.That(Compiler.IsLineEmpty(" "), Is.True);
        Assert.That(Compiler.IsLineEmpty("   "), Is.True);
        Assert.That(Compiler.IsLineEmpty("\t"), Is.True);
        Assert.That(Compiler.IsLineEmpty("\t\t"), Is.True);
        Assert.That(Compiler.IsLineEmpty("\t\t\t"), Is.True);
        Assert.That(Compiler.IsLineEmpty("\n"), Is.True);
        Assert.That(Compiler.IsLineEmpty("A non empty line"), Is.False);
        Assert.That(Compiler.IsLineEmpty("A non empty line with trailing whitespace          "), Is.False);
        Assert.That(Compiler.IsLineEmpty("    A non empty line with leading whitespace"), Is.False);
    }
    
    #endregion
    
    #region Data Block Size Tests

    [Test]
    public void DataBlockSize()
    {
        Assert.That(Compiler.GetDataBlockSize("0"), Is.EqualTo(1));
        Assert.That(Compiler.GetDataBlockSize("1"), Is.EqualTo(1));
        Assert.That(Compiler.GetDataBlockSize("FF020C"), Is.EqualTo(3));
        Assert.That(Compiler.GetDataBlockSize("FF02"), Is.EqualTo(2));
        Assert.That(Compiler.GetDataBlockSize("*4890U93"), Is.EqualTo(7));
    }
    
    #endregion

    #region Get Address From Custom Instruction
    
    [Test]
    public void GetAddressFromCustomInstruction()
    {
        Assert.That(Compiler.GetAddressFromCustomInstruction("loc #1234", out var address), Is.True);
        Assert.That(address, Is.EqualTo(1234));
        Assert.That(Compiler.GetAddressFromCustomInstruction("loc #$1234", out address), Is.True);
        Assert.That(address, Is.EqualTo(0x1234));
        
    }
    
    [Test]
    public void GetAddressFromCustomInstructionInvalid()
    {
        Assert.That(Compiler.GetAddressFromCustomInstruction("data 1234", out var address), Is.False);
        Assert.That(address, Is.Null);
        Assert.That(Compiler.GetAddressFromCustomInstruction("data #1234", out address), Is.False);
        Assert.That(address, Is.Null);
        Assert.That(Compiler.GetAddressFromCustomInstruction("loc #1234 R0", out address), Is.False);
        Assert.That(address, Is.Null);
        Assert.That(Compiler.GetAddressFromCustomInstruction("loc 1234", out address), Is.False);
        Assert.That(address, Is.Null);
    }
    
    [Test]
    public void GetAddressFromCustomInstructionErrors()
    {
        Assert.Throws<FormatException>(() => _ = Compiler.GetAddressFromCustomInstruction("loc #12_34", out _));
        Assert.Throws<FormatException>(() => _ = Compiler.GetAddressFromCustomInstruction("loc #ABCD", out _));
        Assert.Throws<FormatException>(() => _ = Compiler.GetAddressFromCustomInstruction("loc #$-1234", out _));
    }

    #endregion

    #region Strip Comments

    [Test]
    public void StripComments()
    {
        var lines = new[] {
            "loc #1234",
            "data #1234",
            "data #1234 ; hello",
            "data #1234; hello",
            "data #1234; hello world",
        };

        var expected = new[] {
            "loc #1234",
            "data #1234",
            "data #1234 ",
            "data #1234",
            "data #1234",
        };
        
        Assert.That(Compiler.StripComments(lines), Is.EqualTo(expected));
    }

    #endregion
}

public class CompilerInstructionsTests
{
    [Test]
    public void ParseBr()
    {
        Assert.That(Compiler.ParseBr(["BR", "#0"]), Is.EqualTo(new LC3Instruction(0x0000)));
        Assert.That(Compiler.ParseBr(["BRN", "#0"]), Is.EqualTo(new LC3Instruction(0x0800)));
        Assert.That(Compiler.ParseBr(["BRZ", "#0"]), Is.EqualTo(new LC3Instruction(0x0400)));
        Assert.That(Compiler.ParseBr(["BRP", "#0"]), Is.EqualTo(new LC3Instruction(0x0200)));
        Assert.That(Compiler.ParseBr(["BR", "#-1"]), Is.EqualTo(new LC3Instruction(0x01FF)));
    }
    
    [Test]
    public void ParseAdd()
    {
        Assert.That(Compiler.ParseAdd(["ADD", "R0", "R1", "R2"]), Is.EqualTo(new LC3Instruction(0x1042)));
        Assert.That(Compiler.ParseAdd(["ADD", "R1", "R2", "R3"]), Is.EqualTo(new LC3Instruction(0x1283)));
        Assert.That(Compiler.ParseAdd(["ADD", "R1", "R2", "R4"]), Is.EqualTo(new LC3Instruction(0x1284)));
        Assert.That(Compiler.ParseAdd(["ADD", "R6", "R4", "R7"]), Is.EqualTo(new LC3Instruction(0x1D07)));
        
        Assert.That(Compiler.ParseAdd(["ADD", "R0", "R1", "#0"]), Is.EqualTo(new LC3Instruction(0x1060)));
        Assert.That(Compiler.ParseAdd(["ADD", "R1", "R2", "#0"]), Is.EqualTo(new LC3Instruction(0x12A0)));
        Assert.That(Compiler.ParseAdd(["ADD", "R1", "R2", "#-1"]), Is.EqualTo(new LC3Instruction(0x12BF)));
    }
}