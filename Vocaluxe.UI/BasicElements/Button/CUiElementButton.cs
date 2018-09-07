using System.Collections.Generic;
using Vocaluxe.UI.AbstractElements;

namespace Vocaluxe.UI.BasicElements.Button
{
    public class CUiElementButton:CUiElement
    {
        public CControllerElementButton Controller { get; private set; }
        public override CController BasicController => Controller;

        public CUiElementButton()
        {
            Controller = new CControllerElementButton();
        }

        public static CUiElementButton CreateInstance(Dictionary<string, string> properties, IEnumerable<(CUiElement, string)> children)
        {
            return CUiElement.CreateInstance<CUiElementButton>(properties, children);
        }
    }
}
