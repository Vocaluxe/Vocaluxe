using System.Collections.Generic;
using Vocaluxe.UI.AbstractElements;

namespace Vocaluxe.UI.BasicElements.Text
{
    public class CUiElementText:CUiElement
    {
        public CControllerElementText Controller => new CControllerElementText();
        public override CController BasicController => Controller;

        public static CUiElementText CreateInstance(Dictionary<string, string> properties, IEnumerable<(CUiElement uiElement, string bindingId)> children)
        {
            var instance = CUiElement.CreateInstance<CUiElementText>(properties, children);
            properties.TryGetValue("value", out string value);
            instance.Controller.Text = value;
            return instance;
        }
    }
}
