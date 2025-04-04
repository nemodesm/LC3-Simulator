namespace LC3_Simulator;

class Program
{
    static int Main(string[] args)
    {
#if false
        if (!Compiler.Compile("testProgram.lc3asm", out var si))
        {
            
        }
        
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

        var s = new Simulator();
        s.Memory[0x3000] = 0x7482;
        s.Memory[0x300A] = 0x1234;
        s.Memory[0x300B] = 0x2345;
        s.Memory[0x300C] = 0x3456;
        s.Memory[0x300D] = 0x4567;
        s.Memory[0x300E] = 0x5678;
        s.Memory[0x300F] = 0x6789;
        s.R0 = 0x300A;
        s.R1 = 0x3001;
        s.R2 = 0x300B;
        s.R3 = 0x3009;
        s.R4 = 0x3008;
        s.R5 = 0x3001;
        s.R6 = 0x3002;
        s.R7 = 0x3003;
        s.PC = 0x3000;
        s.ExecuteStep();
        Console.WriteLine(s.Memory[0x300A].ToString("X4"));
        Console.WriteLine(s.Memory[0x300B].ToString("X4"));
        Console.WriteLine(s.Memory[0x300C].ToString("X4"));
        Console.WriteLine(s.Memory[0x300D].ToString("X4"));
        Console.WriteLine(s.Memory[0x300E].ToString("X4"));
        Console.WriteLine(s.Memory[0x300F].ToString("X4"));
        Console.WriteLine();
        Console.WriteLine(s.R0.ToString("X4"));
        Console.WriteLine(s.R1.ToString("X4"));
        Console.WriteLine(s.R2.ToString("X4"));
        Console.WriteLine(s.R3.ToString("X4"));
        Console.WriteLine(s.R4.ToString("X4"));
        Console.WriteLine(s.R5.ToString("X4"));
        Console.WriteLine(s.R6.ToString("X4"));
        Console.WriteLine(s.R7.ToString("X4"));
        return 0;
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