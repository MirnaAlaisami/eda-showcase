using System;
using System.Threading.Tasks;
namespace Backend
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Set the location of the downloader <ENTER>: ");
            string argument = Console.ReadLine();
            Downloader s = new Downloader();
            await s.Subscribe(argument);
        }
    }
}
