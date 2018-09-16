using System;
using System.Collections.Generic;
using Vocaluxe.UI.Screens;

namespace Vocaluxe.UI
{
    public static class DummyClass
    {
        // Should be somewhere in Vocaluxe
        public static EUiScreenType CurrentScreen => EUiScreenType.ScreenMain;

        private static readonly Dictionary<string, string> StringData = new Dictionary<string, string>()
        {
            {"btnsingdescription", "Sing"},
            {"btnpartydescription", "Party"},
            {"btnoptionsdescription", "Options"},
            {"btnprofilesdescription", "Profiles"},
            {"btnexitdescription", "Exit"},

        };
        private static readonly Dictionary<string, int> IntData = new Dictionary<string, int>();

        public static T RegisterBinding<T>(string bindingId, EventHandler<T> eventHandler)
        {
            // eventHandler is used to notify about changes -> not implemented yet

            switch (typeof(T).ToString())
            {
                case "System.String":
                    StringData.TryGetValue(bindingId.ToLower(), out string stringResult);
                    return (T)Convert.ChangeType(stringResult, typeof(T));
                case "System.Int32":
                    IntData.TryGetValue(bindingId.ToLower(), out int intResult);
                    return (T)Convert.ChangeType(intResult, typeof(T));
            }

            return default(T);
        }
    }
}
