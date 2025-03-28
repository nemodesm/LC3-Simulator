namespace LC3_Simulator;

class Program
{
    static int Main(string[] args)
    {
#if false
        Console.WriteLine($"{Compiler.IsLabelValid("R0")} == false");
        Console.WriteLine($"{Compiler.IsLabelValid("R1")} == false");
        Console.WriteLine($"{Compiler.IsLabelValid("R3")} == false");
        Console.WriteLine($"{Compiler.IsLabelValid("R7")} == false");
        Console.WriteLine($"{Compiler.IsLabelValid("R8")} == true");
        Console.WriteLine($"{Compiler.IsLabelValid("R10")} == true");
        Console.WriteLine($"{Compiler.IsLabelValid("_")} == false");
        Console.WriteLine($"{Compiler.IsLabelValid("Hello I am a label")} == false");
        Console.WriteLine($"{Compiler.IsLabelValid("HelloIamalabel")} == true");
        Console.WriteLine($"{Compiler.IsLabelValid("\\lebel")} == true");
        Console.WriteLine($"{Compiler.IsLabelValid("kjhg\\lebel2")} == false");

        if (Compiler.Compile("testProgram.lc3asm", "testProgram.tmp"))
        {
            
        }
        
        if(!CompilerV1.Compile("testProgram.lc3asm", "testProgram.bin"))
        {
            return 1;
        }

        var a = File.ReadAllText("testProgram.tmp");
        var b = File.ReadAllText("testProgram.bin");
        if (a != b)
        {
            return 1;
        }

        var sim = new Simulator();
        sim.LoadFromFile("testProgram.bin");
        sim.Memory[0x3003] = 0x3008;
        sim.R0 = 0x0005;
        sim.R1 = 0x000C;
        sim.R2 = 0x0005;
        sim.R3 = 0x0006;
        sim.R4 = 0x0003;
        sim.R5 = 0x0008;
        sim.PC = 0x3000;
        sim.ExecuteStep();
        Console.WriteLine(sim.R0.ToString("X4"));
        Console.WriteLine(sim.R1.ToString("X4"));
        Console.WriteLine(sim.R2.ToString("X4"));
        Console.WriteLine(sim.R3.ToString("X4"));
        Console.WriteLine(sim.R4.ToString("X4"));
        Console.WriteLine(sim.R5.ToString("X4"));
#endif
        if (args.Length == 0)
        {
            Console.WriteLine("No options specified, see --help");
            return 1;
        }
        
        if (args[0] == "--help")
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  lc3sim [options] <input file> [output file]");
            Console.WriteLine("Options:");
            Console.WriteLine("  -r, --run           Run the program");
            Console.WriteLine("  -c, --compile       Compile the program");
            Console.WriteLine("  --help              Show this help message");
            Console.WriteLine("Remarks:");
            Console.WriteLine("  If only -c or --compile is specified, the output file must be specified");
            Console.WriteLine("  If -cr is specified, the output file is optional to save the compiled program");
            return 0;
        }
        
        var run = false;
        var compile = false;

        var options = 0;

        ReadOptions:
        if (args[options][0] == '-')
        {
            if (args[options][1] == '-')
            {
                switch (args[options])
                {
                    case "--run":
                        if (run)
                        {
                            Console.WriteLine("Duplicate option -r");
                            return 1;
                        }
                        run = true;
                        break;
                    case "--compile":
                        if (compile)
                        {
                            Console.WriteLine("Duplicate option -c");
                            return 1;
                        }
                        compile = true;
                        break;
                    default:
                        Console.WriteLine("Unknown option");
                        return 1;
                }
                
                options++;
                goto ReadOptions;
            }
            else
            {
                for (int i = 1; i < args[options].Length; i++)
                {
                    switch (args[options][i])
                    {
                        case 'r':
                            if (run)
                            {
                                Console.WriteLine("Duplicate option -r");
                                return 1;
                            }
                            run = true;
                            break;
                        case 'c':
                            if (compile)
                            {
                                Console.WriteLine("Duplicate option -c");
                                return 1;
                            }
                            compile = true;
                            break;
                        default:
                            Console.WriteLine("Unknown option");
                            return 1;
                    }
                }
            }
        }
        
        if (compile)
        {
            if (args.Length < options + 1)
            {
                Console.WriteLine("Missing input file");
                return 1;
            }

            if (run)
            {
                if (args.Length < options + 2)
                {
                    if (Compiler.Compile(args[options + 1], args[options + 2]))
                    {
                        var sim = new Simulator();
                        sim.LoadFromFile(args[options + 2]);
                        sim.Execute();
                    }
                }
                else if (Compiler.Compile(args[options + 1], out var sim))
                {
                    sim.Execute();
                }
            }
            else
            {
                if (args.Length < options + 2)
                {
                    Console.WriteLine("Missing output file");
                    return 1;
                }
                
                if (Compiler.Compile(args[options + 1], args[options + 2]))
                {
                    Console.WriteLine("Compilation successful");
                }
            }
        }
        else if (run)
        {
            if (args.Length < options + 1)
            {
                Console.WriteLine("Missing input file");
                return 1;
            }
            var sim = new Simulator();
            sim.LoadFromFile(args[options + 1]);
            sim.Execute();
        }

        return 0;
    }
}