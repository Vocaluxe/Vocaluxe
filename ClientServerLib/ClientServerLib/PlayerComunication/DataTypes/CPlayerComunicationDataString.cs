
using System;
using System.Runtime.Serialization;

namespace ServerLib.PlayerComunication.DataTypes
{
    [Serializable]
    public class CPlayerComunicationDataString : CPlayerComunicationData
    {
        [DataMember]
        public string Data { get; set; }
    }
}
