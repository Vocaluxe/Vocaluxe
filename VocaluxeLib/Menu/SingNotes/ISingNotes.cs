using System;
using System.Collections.Generic;
using System.Text;

namespace Vocaluxe.Menu.SingNotes
{
    interface ISingNotes
    {
        void Reset();
        int AddPlayer(SRectF Rect, SColorF Color, int PlayerNr);
        void RemovePlayer(int ID);


        void AddLine(int ID, CLine[] Line, int LineNr, int Player);
        void RemoveLine(int ID);

        void AddNote(int ID, CNote Note);

        void SetAlpha(int ID, float Alpha);
        float GetAlpha(int ID);

        void Draw(int ID, int Player);
        void Draw(int ID, List<CLine> SingLine, int Player);
    }
}
