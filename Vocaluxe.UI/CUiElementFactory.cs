using System;
using System.Collections.Generic;
using Vocaluxe.UI.AbstractElements;

namespace Vocaluxe.UI
{
    public static class CUiElementFactory
    {
        private delegate CUiElement CreateElementDelgate(Dictionary<string, string> properties, IEnumerable<(CUiElement uiElement, string bindingId)> children);

        private static readonly Dictionary<string, CreateElementDelgate> _Mapping = new Dictionary<string, CreateElementDelgate>()
        {
            {"screenmain", Screens.ScreenMain.CUiScreenMain.CreateInstance},
            {"button", BasicElements.Button.CUiElementButton.CreateInstance},
            {"text", BasicElements.Text.CUiElementText.CreateInstance}
        };

        public static CUiElement CreateElement(string type, Dictionary<string, string> properties, IEnumerable<(CUiElement uiElement, string bindingId)> children)
        {
            _Mapping.TryGetValue(type.ToLower(), out var factoryFunction);

            if (factoryFunction == null)
            {
                throw new ArgumentException("The given id does not match any known element type");
            }

            return factoryFunction(properties, children);
        }
    }
}
