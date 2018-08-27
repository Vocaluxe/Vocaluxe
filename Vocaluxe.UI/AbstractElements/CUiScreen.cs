using System.Collections.Generic;
using Vocaluxe.UI.Screens;

namespace Vocaluxe.UI.AbstractElements
{
    public abstract class CUiScreen:CUiElement
    {
        public void Resize(int width, int height)
        {
            Width = width;
            Height = height;

            CalculateLayout();
        }

        public abstract EUiScreenType ScreenType { get; }
    }
}
