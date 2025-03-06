namespace LC3_Simulator;

class Program
{
    static int Main(string[] args)
    {
        if(!Compiler.Compile("testProgram.asm", "testProgram.bin"))
        {
            return 1;
        }
        var sim = new Simulator();
        sim.LoadFromFile("testProgram.bin");
        sim.Execute();

        return 0;
    }
}