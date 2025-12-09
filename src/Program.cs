using System;
using RNG_Daemon;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            var core = new Core();
            core.Run(args);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Fatal error: " + ex.Message);
        }
    }
}

