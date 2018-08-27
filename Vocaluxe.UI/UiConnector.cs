using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Vocaluxe.UI.AbstractElements;
using Vocaluxe.UI.Parser;
using Vocaluxe.UI.Screens;

namespace Vocaluxe.UI
{
    // Fasade
    public class CUiConnector
    {
        private readonly CUiScreenStore _Store = new CUiScreenStore();

        public CUiConnector(int width, int height)
        {
            _Store.LoadScreens(@"D:\ProfileFolders\Desktop\Screens", (width,height));
        }

        public Dictionary<EUiScreenType, CUiScreen> GetAllScreens()
        {
            return _Store.GetAllScreen();
        }

        public EUiScreenType GetCurrentScreen()
        {
            return DummyClass.CurrentScreen;
        }

        // Dummy to fake a call
        public void ChangeScreen(EUiScreenType newScreen)
        {
            OnScreenChange?.Invoke(this, DummyClass.CurrentScreen);
        }

        public event EventHandler<EUiScreenType> OnScreenChange;


        #region oldStuff
        public CUiScreen GetCurrentScreenOld()
        {
            return _Store[DummyClass.CurrentScreen];
        }

        // Dummy
        public void ChangeScreenOld(EUiScreenType newScreen)
        {
            OnScreenChangeOld?.Invoke(this, _Store[DummyClass.CurrentScreen]);
        }

        public event EventHandler<CUiScreen> OnScreenChangeOld;
        #endregion
    }
}
