// PortOpener
using System.Net;
using System.Net.Sockets;
using Mono.Nat;
using UnityEngine;

public class PortOpener : MonoBehaviour
{
	private INatDevice device;

	public bool UpnpWorks { get; private set; } = false;
	public bool Checked = false;

	public string LocalIp
	{
		get
		{
			IPAddress[] addressList = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
			foreach (IPAddress iPAddress in addressList)
			{
				if (iPAddress.AddressFamily == AddressFamily.InterNetwork)
				{
					return iPAddress.ToString();
				}
			}
			return null;
		}
	}

	public string GlobalIp
	{
		get
		{
			if (UpnpWorks)
			{
				return device.GetExternalIP().ToString();
			}
			return null;
		}
	}

	public void Start()
	{
		NatUtility.DeviceFound += DeviceFound;
		NatUtility.StartDiscovery();
	}

	public void OpenPort(int port, int publicPort)
	{
		device.CreatePortMap(new Mapping(Protocol.Udp, port, publicPort));
	}

	private void DeviceFound(object sender, DeviceEventArgs args)
	{
		if (device == null)
		{
			device = args.Device;
			UpnpWorks = device.GetExternalIP().ToString() == new WebClient().DownloadString("http://icanhazip.com");
		}
	}
}
