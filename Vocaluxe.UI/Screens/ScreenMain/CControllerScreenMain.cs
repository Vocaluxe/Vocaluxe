using System;
using Vocaluxe.UI.AbstractElements;
using Vocaluxe.UI.BasicElements.Button;

namespace Vocaluxe.UI.Screens.ScreenMain
{
    public class CControllerScreenMain: CController
    {
        private CControllerElementButton _BtnSingController;
        private CControllerElementButton _BtnPartyController;
        private CControllerElementButton _BtnOptionsController;
        private CControllerElementButton _BtnProfilesController;
        private CControllerElementButton _BtnExitController;

        public override void RegisterChild(CController child, string id)
        {
            Enum.TryParse(id, true, out EScreenMainComponents parsedId);

            switch (parsedId)
            {
                case EScreenMainComponents.BtnSing:
                    _BtnSingController = child as CControllerElementButton ?? throw new ArgumentException("BtnSing must be a button element");
                    _BtnSingController.Clicked += BtnSingClicked;
                    break;
                case EScreenMainComponents.BtnParty:
                    _BtnPartyController = child as CControllerElementButton ?? throw new ArgumentException("BtnParty must be a button element");
                    _BtnPartyController.Clicked += BtnPartyClicked;
                    break;
                case EScreenMainComponents.BtnOptions:
                    _BtnOptionsController = child as CControllerElementButton ?? throw new ArgumentException("BtnOptions must be a button element");
                    _BtnOptionsController.Clicked += BtnOptionsClicked;
                    break;
                case EScreenMainComponents.BtnProfiles:
                    _BtnProfilesController = child as CControllerElementButton ?? throw new ArgumentException("BtnProfiles must be a button element");
                    _BtnProfilesController.Clicked += BtnProfilesClicked;
                    break;
                case EScreenMainComponents.BtnExit:
                    _BtnExitController = child as CControllerElementButton ?? throw new ArgumentException("BtnExit must be a button element");
                    _BtnExitController.Clicked += BtnExitClicked;
                    break;
                case EScreenMainComponents.Unknown:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public event EventHandler BtnSingClicked;
        public event EventHandler BtnPartyClicked;
        public event EventHandler BtnOptionsClicked;
        public event EventHandler BtnProfilesClicked;
        public event EventHandler BtnExitClicked;

    }
}
