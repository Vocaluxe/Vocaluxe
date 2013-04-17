using System.Collections.Generic;

namespace VocaluxeLib.Menu.SingNotes
{
    public interface ISingNotes
    {
        void Reset();
        int AddPlayer(SRectF rect, SColorF color, int playerNr);
        void RemovePlayer(int iD);

        void AddLine(int iD, CLine[] line, int lineNr, int player);
        void RemoveLine(int iD);

        void AddNote(int iD, CNote note);

        void SetAlpha(int iD, float alpha);
        float GetAlpha(int iD);

        void Draw(int iD, int player);
        void Draw(int iD, List<CLine> singLine, int player);
    }
}