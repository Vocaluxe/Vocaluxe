using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Vocaluxe.UI.AbstractElements;
using Vocaluxe.UI.Screens;

namespace Vocaluxe.UI.Parser
{
    public class CUiScreenStore
    {
        private readonly Dictionary<EUiScreenType, CUiScreen> _LoadedScreens = new Dictionary<EUiScreenType, CUiScreen>();

        public void LoadScreens(string path, (int width, int height) initalialSize)
        {
            if(!Directory.Exists(path))
                throw new DirectoryNotFoundException("Screen file path must exist");

            _LoadedScreens.Clear();

            IEnumerable<string> filePaths = Directory.GetFiles(path, "*.xml");

            foreach (var filePath in filePaths)
            {
                CUiScreen screen = CUiScreenParser.Parse(filePath);
                if(initalialSize.width > 0 && initalialSize.height > 0)
                    screen.Resize(initalialSize.width, initalialSize.height);
                _LoadedScreens.Add(screen.ScreenType, screen);
            }
        }

        public Dictionary<EUiScreenType, CUiScreen> GetAllScreen()
        {
            // ReadOnlyDictionary would be better but got problems in unity
            return _LoadedScreens;
        }

        public CUiScreen GetScreen(EUiScreenType screenType)
        {
            _LoadedScreens.TryGetValue(screenType, out CUiScreen screen);
            return screen;
        }

        public CUiScreen this[EUiScreenType screenType] => GetScreen(screenType);
    }
}
