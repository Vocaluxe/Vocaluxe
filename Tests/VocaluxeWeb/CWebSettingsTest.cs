#region license
// This file is part of Vocaluxe.
// 
// Vocaluxe is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Vocaluxe is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using VocaluxeWeb.Contracts;
using VocaluxeWeb.Remote;
using VocaluxeWeb.Services;

namespace Tests.VocaluxeWeb
{
    [TestFixture]
    public class CWebSettingsTest
    {
        private string _Path;

        [SetUp]
        public void SetUp()
        {
            _Path = Path.Combine(Path.GetTempPath(), "VocaluxeWebTest_" + Guid.NewGuid().ToString("N"), "WebSettings.json");
        }

        [TearDown]
        public void TearDown()
        {
            var directory = Path.GetDirectoryName(_Path);
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }

        [Test]
        public void TestAdminHasAllPermissions()
        {
            var settings = new CWebSettings();
            settings.GuestPermissions.Clear();
            settings.PlayerPermissions.Clear();
            var all = Enum.GetValues(typeof(EPermission)).Cast<EPermission>().ToList();

            Assert.That(settings.GetPermissions(ERemoteRole.Admin), Is.EquivalentTo(all));
            Assert.That(settings.GetPermissions(ERemoteRole.Guest), Is.Empty);
            Assert.That(settings.GetPermissions(ERemoteRole.Player), Is.Empty);
        }

        [Test]
        public void TestDefaults()
        {
            var settings = new CWebSettings();
            Assert.That(settings.GetPermissions(ERemoteRole.Guest), Is.EquivalentTo(new[] { EPermission.AddToQueue }));
            Assert.That(settings.GetPermissions(ERemoteRole.Player), Does.Contain(EPermission.AddToQueue));
            Assert.That(settings.GetPermissions(ERemoteRole.Player), Does.Not.Contain(EPermission.EditAllProfiles));
        }

        [Test]
        public void TestStoreSavesAndLoads()
        {
            var store = new CWebSettingsStore(_Path);
            store.Update(s =>
            {
                s.AllowProfileCreation = false;
                s.QueuePlaylistName = "Party queue";
                s.GuestPermissions.Add(EPermission.UseKeyboard);
            });

            var loaded = new CWebSettingsStore(_Path).Current;
            Assert.That(loaded.AllowProfileCreation, Is.False);
            Assert.That(loaded.QueuePlaylistName, Is.EqualTo("Party queue"));
            Assert.That(loaded.GetPermissions(ERemoteRole.Guest), Does.Contain(EPermission.UseKeyboard));
        }

        [Test]
        public void TestCurrentIsACopy()
        {
            var store = new CWebSettingsStore(_Path);
            store.Current.GuestPermissions.Add(EPermission.DeletePlaylists);
            Assert.That(store.Current.GuestPermissions, Does.Not.Contain(EPermission.DeletePlaylists));
        }

        [Test]
        public void TestBrokenFile()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_Path));
            File.WriteAllText(_Path, "{ broken");
            var store = new CWebSettingsStore(_Path);
            Assert.That(store.Current.QueuePlaylistName, Is.EqualTo("WebApp Queue"));
        }

        [Test]
        public void TestFileWithRemovedSettings()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_Path));
            File.WriteAllText(_Path, "{ \"RequireJoinToken\": true, \"JoinToken\": \"abc\", \"QueuePlaylistName\": \"Party\" }");
            Assert.That(new CWebSettingsStore(_Path).Current.QueuePlaylistName, Is.EqualTo("Party"));
        }
    }
}
