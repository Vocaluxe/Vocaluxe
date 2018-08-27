using System;
using Vocaluxe.UI.AbstractElements;
using Vocaluxe.UI.BasicElements.Text;

namespace Vocaluxe.UI.BasicElements.Button
{
    public class CControllerElementButton: CController
    {
        private CControllerElementText _DescriptionController;

        public override void RegisterChild(CController child, string id)
        {
            Enum.TryParse(id, out EElementButtonComponents parsedId);

            switch (parsedId)
            {
                case EElementButtonComponents.TxtDescription:
                    _DescriptionController = child as CControllerElementText ?? throw new ArgumentException("TxtDescription must be a text element");
                    break;
                case EElementButtonComponents.Unknown:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public event EventHandler Clicked;

        public void RaiseClicked()
        {
            Clicked?.Invoke(this, null);
        }

        public string Description
        {
            get => _DescriptionController.Text;
            set => _DescriptionController.Text = value;
        }
    }
}
