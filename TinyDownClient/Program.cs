using Microsoft.Extensions.Configuration;

namespace TinyDownClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
              .AddCommandLine(args)
              .Build();

            Console.WriteLine($"path: {config["path"]}");
            Console.WriteLine($"other: {config["other"]}");
            Console.ReadLine();
        }
    }
}
