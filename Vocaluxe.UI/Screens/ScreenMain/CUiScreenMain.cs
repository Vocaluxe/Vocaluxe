using System.Collections.Generic;
using Vocaluxe.UI.AbstractElements;

namespace Vocaluxe.UI.Screens.ScreenMain
{
    public class CUiScreenMain:CUiScreen
    {
        public CControllerScreenMain Controller { get; private set; }
        public override CController BasicController => Controller;

        public CUiScreenMain()
        {
            Controller = new CControllerScreenMain();
        }

        public static CUiScreen CreateInstance(Dictionary<string, string> properties, IEnumerable<(CUiElement uiElement, string bindingId)> children)
        {
            return CUiElement.CreateInstance<CUiScreenMain>(properties, children);
        }

        public override EUiScreenType ScreenType => EUiScreenType.ScreenMain;
    }
}
