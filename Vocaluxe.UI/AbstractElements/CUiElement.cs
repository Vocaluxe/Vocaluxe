using System;
using System.Collections.Generic;
using Facebook.Yoga;

namespace Vocaluxe.UI.AbstractElements
{
    public abstract class CUiElement : YogaNode
    {
        private static YogaConfig _Config = new YogaConfig();
        
        #region Add Z component

        private int _Z = -1;

        public int LayoutZ
        {
            get => _Z;
            set => _Z = value;
        }

        #endregion

        public abstract CController BasicController { get; }

        public static T CreateInstance<T> (Dictionary<string, string> properties, IEnumerable<(CUiElement uiElement, string bindingId)> children) where T : CUiElement, new()
        {
            var newInstance = new T();

            _SetYogaProperties(newInstance, properties);

            foreach ((CUiElement uiElement, string bindingId) in children)
            {
                newInstance.AddChild(uiElement);
                newInstance.BasicController.RegisterChild(uiElement.BasicController, bindingId);
            }

            return newInstance;
        }

        private static void _SetYogaProperties(CUiElement targetElement, Dictionary<string, string> properties)
        {
            foreach (KeyValuePair<string, string> keyValue in properties)
            {
                switch (keyValue.Key.ToLower())
                {
                    case "width":
                        targetElement.Width = Int32.Parse(keyValue.Value);
                        break;
                    case "height":
                        targetElement.Height = Int32.Parse(keyValue.Value);
                        break;
                    case "flexdirection":
                        Enum.TryParse(keyValue.Value, true, out YogaFlexDirection flexDirection);
                        targetElement.FlexDirection = flexDirection;
                        break;
                    case "wrap":
                        Enum.TryParse(keyValue.Value, true, out YogaWrap wrap);
                        targetElement.Wrap = wrap;
                        break;
                    case "aligncontent":
                        Enum.TryParse(keyValue.Value, true, out YogaAlign alignContent);
                        targetElement.AlignContent = alignContent;
                        break;
                    case "alignitems":
                        Enum.TryParse(keyValue.Value, true, out YogaAlign alignItems);
                        targetElement.AlignItems = alignItems;
                        break;
                    case "alignself":
                        Enum.TryParse(keyValue.Value, true, out YogaAlign alignSelf);
                        targetElement.AlignSelf = alignSelf;
                        break;
                    case "positiontype":
                        Enum.TryParse(keyValue.Value, true, out YogaPositionType positionType);
                        targetElement.PositionType = positionType;
                        break;
                    case "aspectratio":
                        targetElement.AspectRatio = float.Parse(keyValue.Value);
                        break;
                    case "flex":
                        targetElement.Flex = float.Parse(keyValue.Value);
                        break;
                    case "flexbasis":
                        targetElement.FlexBasis = float.Parse(keyValue.Value);
                        break;
                    case "flexgrow":
                        targetElement.FlexGrow = float.Parse(keyValue.Value);
                        break;
                    case "flexshrink":
                        targetElement.FlexShrink = float.Parse(keyValue.Value);
                        break;
                    case "justifycontent":
                        Enum.TryParse(keyValue.Value, true, out YogaJustify justifyContent);
                        targetElement.JustifyContent = justifyContent;
                        break;
                    case "margin":
                        targetElement.Margin = float.Parse(keyValue.Value);
                        break;
                    case "marginbottom":
                        targetElement.MarginBottom = float.Parse(keyValue.Value);
                        break;
                    case "margintop":
                        targetElement.MarginTop = float.Parse(keyValue.Value);
                        break;
                    case "marginleft":
                        targetElement.MarginLeft = float.Parse(keyValue.Value);
                        break;
                    case "marginright":
                        targetElement.MarginRight = float.Parse(keyValue.Value);
                        break;
                    case "padding":
                        targetElement.Padding = float.Parse(keyValue.Value);
                        break;
                    case "paddingbottom":
                        targetElement.PaddingBottom = float.Parse(keyValue.Value);
                        break;
                    case "paddingtop":
                        targetElement.PaddingTop = float.Parse(keyValue.Value);
                        break;
                    case "paddingleft":
                        targetElement.PaddingLeft = float.Parse(keyValue.Value);
                        break;
                    case "paddingright":
                        targetElement.PaddingRight = float.Parse(keyValue.Value);
                        break;
                    case "borderwidth":
                        targetElement.BorderWidth = float.Parse(keyValue.Value);
                        break;
                    case "borderbottomwidth":
                        targetElement.BorderBottomWidth = float.Parse(keyValue.Value);
                        break;
                    case "bordertopwidth":
                        targetElement.BorderTopWidth = float.Parse(keyValue.Value);
                        break;
                    case "borderleftwidth":
                        targetElement.BorderLeftWidth = float.Parse(keyValue.Value);
                        break;
                    case "borderrightwidth":
                        targetElement.BorderRightWidth = float.Parse(keyValue.Value);
                        break;
                    case "minwidth":
                        targetElement.MinWidth = float.Parse(keyValue.Value);
                        break;
                    case "maxwidth":
                        targetElement.MaxWidth = float.Parse(keyValue.Value);
                        break;
                    case "minheight":
                        targetElement.MinHeight = float.Parse(keyValue.Value);
                        break;
                    case "maxheight":
                        targetElement.MaxHeight = float.Parse(keyValue.Value);
                        break;
                }
            }
        }
    }
}
