using System.Xml;

namespace VocaluxeLib.Menu
{
    interface IMenuElement
    {
        string GetThemeName();

        bool LoadTheme(string xmlPath, string elementName, CXMLReader xmlReader, int skinIndex);
        bool SaveTheme(XmlWriter writer);

        void UnloadTextures();
        void LoadTextures();
        void ReloadTextures();

        void MoveElement(int stepX, int stepY);
        void ResizeElement(int stepW, int stepH);
    }
}