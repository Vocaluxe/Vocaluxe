using System;
using Vocaluxe.UI.Parser;
using Vocaluxe.UI.Screens;

namespace Dummy
{
    class Program
    {
        static void Main(string[] args)
        {
            var store = new CUiScreenStore();
            store.LoadScreens(@"D:\ProfileFolders\Desktop\Screens", (800,600));
            Console.ReadKey();
        }
    }
}
