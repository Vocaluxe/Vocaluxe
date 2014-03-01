using ServerLib.PlayerComunication.DataTypes;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Web.Script.Serialization;

namespace ServerLib.PlayerComunication
{
    public static class CPlayerCommunication
    {
        public delegate void IncommingMessageDelegate(CPlayerComunicationData data);

        private static readonly Dictionary<int, List<SPlayerComunicationFrame>> _OutgoingMessagesPerUser
            = new Dictionary<int, List<SPlayerComunicationFrame>>();

        private static readonly Queue<SPlayerComunicationFrame> _IncommingMessages = new Queue<SPlayerComunicationFrame>();

        private static readonly Dictionary<EPlayerComunicationType, List<IncommingMessageDelegate>> _IncommingMessageHandler
            = new Dictionary<EPlayerComunicationType, List<IncommingMessageDelegate>>();

        private static readonly Dictionary<EPlayerComunicationType, List<int>> _Subscriptions
            = new Dictionary<EPlayerComunicationType, List<int>>();

        private static readonly Dictionary<EPlayerComunicationType, Type> _TypeMapping
            = new Dictionary<EPlayerComunicationType, Type>();

        internal static void Init()
        {
            SetDataType(EPlayerComunicationType.RegisterSubscription, typeof(CPlayerComunicationDataSubscription));
            AddIncommingHandler(EPlayerComunicationType.RegisterSubscription,
                data => _AddSubscription(((CPlayerComunicationDataSubscription)data).Type,
                    ((CPlayerComunicationDataSubscription)data).PlayerId));

            SetDataType(EPlayerComunicationType.UnregisterSubscription, typeof(CPlayerComunicationDataSubscription));
            AddIncommingHandler(EPlayerComunicationType.UnregisterSubscription,
                data => _RemoveSubscription(((CPlayerComunicationDataSubscription)data).Type,
                    ((CPlayerComunicationDataSubscription)data).PlayerId));
        }

        public static void AddNewPlayerMessage(int targetPlayerId,
            EPlayerComunicationType type, ISerializable data,
            DateTime validTill)
        {
            if (!_OutgoingMessagesPerUser.ContainsKey(targetPlayerId))
            {
                _OutgoingMessagesPerUser.Add(targetPlayerId, new List<SPlayerComunicationFrame>(2));
            }

            _OutgoingMessagesPerUser[targetPlayerId].Add(new SPlayerComunicationFrame
            {
                ProfileId = targetPlayerId,
                Type = type,
                Data = _SerializeHelper(data),
                ValidTill = validTill
            });
        }

        public static void AddNewPlayerMessage(int tagertPlayerId,
           EPlayerComunicationType type, ISerializable data)
        {
            AddNewPlayerMessage(tagertPlayerId, type, data, DateTime.Now.AddMinutes(1));
        }

        public static void AddNewPlayerMessage(EPlayerComunicationType type, ISerializable data,
            DateTime validTill)
        {
            if (_Subscriptions.ContainsKey(type))
            {
                foreach (int subscribedPlayers in _Subscriptions[type])
                {
                    AddNewPlayerMessage(subscribedPlayers, type, data, validTill);
                }
            }

        }

        public static void AddNewPlayerMessage(EPlayerComunicationType type, ISerializable data)
        {
            AddNewPlayerMessage(type, data, DateTime.Now.AddMinutes(1));
        }

        internal static SPlayerComunicationFrame[] GetAllMessagesForPlayer(int playerId)
        {
            SPlayerComunicationFrame[] messages = null;
            if (_OutgoingMessagesPerUser.ContainsKey(playerId))
            {
                messages = _OutgoingMessagesPerUser[playerId].ToArray();
                _OutgoingMessagesPerUser[playerId].Clear();
            }
            return new SPlayerComunicationFrame[]{
                 new SPlayerComunicationFrame()
                 {
                     ProfileId = 0,
                     Type = EPlayerComunicationType.RegisterSubscription,
                     ValidTill = DateTime.Now.AddMinutes(1),
                     Data = _SerializeHelper(new CPlayerComunicationDataString(){Data = "Test"})
                 }
             };
            //return messages;
        }

        public static void RemoveAllMessages(int playerId, EPlayerComunicationType type)
        {
            if (_OutgoingMessagesPerUser.ContainsKey(playerId))
            {
                _OutgoingMessagesPerUser[playerId].RemoveAll(m => m.Type == type);
            }
        }

        internal static void AddMessagesFromPlayer(IEnumerable<SPlayerComunicationFrame> messages, int playerId)
        {
            if (messages == null || playerId < 0)
            {
                return;
            }

            foreach (SPlayerComunicationFrame message in messages)
            {
                SPlayerComunicationFrame data = message;
                data.ProfileId = playerId;
                _IncommingMessages.Enqueue(data);
            }

            if (_IncommingMessages.Count > 0)
            {
                ThreadPool.QueueUserWorkItem(_ProcessIncommingQueue);
            }
        }

        private static void _ProcessIncommingQueue(object dummy)
        {
            lock (_IncommingMessages)
            {
                foreach (SPlayerComunicationFrame message in _IncommingMessages)
                {
                    if (_IncommingMessageHandler.ContainsKey(message.Type))
                    {
                        List<IncommingMessageDelegate> handlers = _IncommingMessageHandler[message.Type];
                        var deserializedData = _DeserializeHelper(message.Data, message.Type);
                        foreach (IncommingMessageDelegate handler in handlers)
                        {
                            handler(deserializedData);
                        }
                    }
                }
            }
        }

        public static void SetDataType(EPlayerComunicationType type, Type dataType)
        {
            if (_TypeMapping.ContainsKey(type))
            {
                _TypeMapping.Add(type, dataType);
            }
            else
            {
                _TypeMapping[type] = dataType;
            }
        }

        public static void AddIncommingHandler(EPlayerComunicationType type, IncommingMessageDelegate handler)
        {
            if (!_IncommingMessageHandler.ContainsKey(type))
            {
                _IncommingMessageHandler.Add(type, new List<IncommingMessageDelegate>(2));
            }

            _IncommingMessageHandler[type].Add(handler);
        }

        private static void _AddSubscription(EPlayerComunicationType type, int playerId)
        {
            if (!_Subscriptions.ContainsKey(type))
            {
                _Subscriptions.Add(type, new List<int>(2));
            }
            if (!_Subscriptions[type].Contains(playerId))
            {
                _Subscriptions[type].Add(playerId);
            }
        }

        private static void _RemoveSubscription(EPlayerComunicationType type, int playerId)
        {
            if (_Subscriptions.ContainsKey(type) && _Subscriptions[type].Contains(playerId))
            {
                _Subscriptions[type].Remove(playerId);
            }
        }

        private static CPlayerComunicationData _DeserializeHelper(String data, EPlayerComunicationType type)
        {
            if (_TypeMapping.ContainsKey(type))
            {
                return (CPlayerComunicationData)(new JavaScriptSerializer()).Deserialize(data, _TypeMapping[type]);
            }
            else
            {
                return new CPlayerComunicationDataString { Data = data };
            }
        }

        private static string _SerializeHelper(Object data)
        {
            return (new JavaScriptSerializer()).Serialize(data);
        }
    }
}
