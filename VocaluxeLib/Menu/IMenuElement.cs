using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Vocaluxe.Menu
{
    interface IMenuElement
    {
        string GetThemeName();

        bool LoadTheme(string XmlPath, string ElementName, CXMLReader xmlReader, int SkinIndex);
        bool SaveTheme(XmlWriter writer);

        void UnloadTextures();
        void LoadTextures();
        void ReloadTextures();
        
        void MoveElement(int stepX, int stepY);
        void ResizeElement(int stepW, int stepH);
    }
}
