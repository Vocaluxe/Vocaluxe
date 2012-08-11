using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Xml.XPath;

using Vocaluxe.Base;
using Vocaluxe.Lib.Draw;

namespace Vocaluxe.Menu.Animations
{
    interface IAnimation
    {
        void Init();
        bool LoadAnimation(string item, XPathNavigator navigator);
        bool SaveAnimation(XmlWriter writer);

        void setRect(SRectF rect);
        SRectF getRect();
        void setColor(SColorF color);
        SColorF getColor();

        void StartAnimation();
        void StopAnimation();
        void ResetAnimation();
        bool AnimationActive();
        void Update();
    }
}
