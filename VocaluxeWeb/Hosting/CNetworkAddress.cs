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

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace VocaluxeWeb.Hosting
{
    static class CNetworkAddress
    {
        /// IPv4 addresses of this machine in the local network, best candidate first.
        /// Interfaces with a default gateway (the "real" network) are preferred over virtual adapters.
        public static IList<IPAddress> GetLanAddresses()
        {
            return (from nic in NetworkInterface.GetAllNetworkInterfaces()
                    where nic.OperationalStatus == OperationalStatus.Up
                          && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback
                          && nic.NetworkInterfaceType != NetworkInterfaceType.Tunnel
                    let props = nic.GetIPProperties()
                    let hasGateway = props.GatewayAddresses.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork && !g.Address.Equals(IPAddress.Any))
                    from address in props.UnicastAddresses
                    where address.Address.AddressFamily == AddressFamily.InterNetwork
                    orderby hasGateway descending
                    select address.Address).ToList();
        }
    }
}
