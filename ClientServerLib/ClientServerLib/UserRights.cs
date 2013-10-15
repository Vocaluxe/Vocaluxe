using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerLib
{
    [Flags]
    internal enum UserRights
    {
        None = 0x00,
        EditAllProfiles = 0x01,
        UploadPhotos = 0x02,
        ViewOtherProfiles = 0x04,
        UseKeyboard = 0x08
    }
}
