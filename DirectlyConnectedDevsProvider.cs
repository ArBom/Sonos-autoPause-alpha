using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.NetworkInformation;

namespace Sonos_autoPause_alpha
{
    internal class DirectlyConnectedDevsProvider
    {
        public List<PhysicalAddress> physicalAddressesToInfo = new List<PhysicalAddress>();
        public List<IPAddress> allSonosAddresses;

        public DirectlyConnectedDevsProvider(List<IPAddress> allSonosAddresses)
        {
            NetworkChange.NetworkAddressChanged += new NetworkAddressChangedEventHandler(AddressChangedCallback);
            AddressChangedCallback();
            this.allSonosAddresses = allSonosAddresses;
        }

        public void AddressChangedCallback(object sender = null, EventArgs e = null)
        {
            if (allSonosAddresses is null)
            {
                physicalAddressesToInfo = new List<PhysicalAddress>();
                return;
            }

            if (allSonosAddresses.Count() == 0)
            {
                physicalAddressesToInfo = new List<PhysicalAddress>();
                return;
            }

            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            List<PhysicalAddress> PhysicalAddresses = new List<PhysicalAddress>();

            foreach (NetworkInterface n in adapters)
            {
                if (n.NetworkInterfaceType == NetworkInterfaceType.Ethernet || n.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet)
                {
                    PhysicalAddresses.Add(n.GetPhysicalAddress());
                }
            }

        }

        ~DirectlyConnectedDevsProvider()
        {
            NetworkChange.NetworkAddressChanged -= new NetworkAddressChangedEventHandler(AddressChangedCallback);
        }
    }
}
