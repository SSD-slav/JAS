// NetworkGameManager
using System.Collections.Generic;
using System.Net;
using Unity.Netcode;
using TMPro;
using Unity.Netcode.Transports.UNET;
using UnityEngine;
using UnityEngine.UI;

public class NetworkGameManager : MonoBehaviour
{
	private PortOpener portOpener;

	private NetworkDiscovery networkDiscovery;

	[SerializeField]
	private TMP_InputField ip;

	[SerializeField]
	private TMP_InputField serverName;

	[SerializeField]
	private GameObject MainMenu;

	[SerializeField]
	private GameObject GameCanvas;

	[SerializeField]
	private GameObject Content;

	[SerializeField]
	private GameObject ServerPanel;

	[SerializeField]
	private UNetTransport transport;

	[SerializeField]
	private Button hostButton;

	[SerializeField]
	private Toggle UseUPNP;

	private Dictionary<IPAddress, DiscoveryResponseData> discoveredServers = new Dictionary<IPAddress, DiscoveryResponseData>();

	[SerializeField]
	public List<IPAddress> addresses = new List<IPAddress>();

	private void Server()
	{
		transport.ConnectPort = 7777;
		transport.ServerListenPort = 7777;
		NetworkManager.Singleton.StartServer();
	}

	public void Client()
	{
		if (!ip.text.Contains(":"))
		{
			ip.text += ":7777";
		}
		transport.ConnectAddress = ip.text.Split(':')[0];
		transport.ServerListenPort = int.Parse(ip.text.Split(':')[1]);
		transport.ConnectPort = int.Parse(ip.text.Split(':')[1]);
		StopDiscovering();
		NetworkManager.Singleton.StartClient();
		MainMenu.SetActive(value: false);
		GameCanvas.SetActive(value: true);
	}

	public void Host()
	{
		if (!ip.text.Contains(":"))
		{
			ip.text += ":7777";
		}
		transport.ConnectPort = int.Parse(ip.text.Split(':')[1]);
		transport.ServerListenPort = int.Parse(ip.text.Split(':')[1]);
		if (UseUPNP.isOn)
		{
			portOpener.OpenPort(int.Parse(ip.text.Split(':')[1]), int.Parse(ip.text.Split(':')[1]));
		}
		networkDiscovery.ServerName = serverName.text;
		StopDiscovering();
		networkDiscovery.StartServer();
		NetworkManager.Singleton.StartHost();
		MainMenu.SetActive(value: false);
		GameCanvas.SetActive(value: true);
	}

	private void Start()
	{
		if (!PlayerPrefs.HasKey("useUPNP"))
		{
			PlayerPrefs.SetString("useUPNP", true.ToString());
		}

		UseUPNP.isOn = bool.Parse(PlayerPrefs.GetString("useUPNP"));
		
		if (!PlayerPrefs.HasKey("LastIP"))
		{
			PlayerPrefs.SetString("LastIP", portOpener.LocalIp);
		}

		ip.text = PlayerPrefs.GetString("LastIP");
		Screen.sleepTimeout = -1;
		portOpener = GetComponent<PortOpener>();
		networkDiscovery = GetComponent<NetworkDiscovery>();
		transport = GetComponent<UNetTransport>();
		StartDiscovering();
		
		InvokeRepeating("UseUpnpChange",0.1f,0.1f);
	}

	private void RefreshList()
	{
		for (int i = 0; i < Content.transform.childCount; i++)
		{
			Destroy(Content.transform.GetChild(i).gameObject);
		}
		for (int i = 0; i < addresses.Count; i++)
		{
			GameObject obj = Instantiate(ServerPanel, Content.transform);
			obj.transform.GetChild(0).GetComponent<TMP_Text>().text = discoveredServers[addresses[i]].ServerName;
			int temp = i;
			obj.transform.GetChild(1).GetComponent<Button>().onClick.AddListener(delegate
			{
				JoinServer(temp);
			});
		}
	}

	public void SaveIP()
	{
		PlayerPrefs.SetString("LastIP", ip.text);
	}
	private void JoinServer(int server)
	{
		ip.text = addresses[server] + ":" + discoveredServers[addresses[server]].Port;
		transport.ConnectPort = discoveredServers[addresses[server]].Port;
		Client();
	}

	public void onServerFound(IPEndPoint sender, DiscoveryResponseData response)
	{
		discoveredServers[sender.Address] = response;
		if (!addresses.Contains(sender.Address))
		{
			addresses.Add(sender.Address);
		}
		RefreshList();
		
	}

	public void StartDiscovering()
	{
		InvokeRepeating("Refresh", 0f, 2f);
		networkDiscovery.StartClient();
		networkDiscovery.ClientBroadcast(default(DiscoveryBroadcastData));
	}

	public void StopDiscovering()
	{
		CancelInvoke("Refresh");
		networkDiscovery.StopDiscovery();
		discoveredServers.Clear();
		addresses.Clear();
	}

	private void Refresh()
	{
		discoveredServers.Clear();
		addresses.Clear();
		networkDiscovery.ClientBroadcast(default(DiscoveryBroadcastData));
	}

	public void UseUpnpChange()
	{
		PlayerPrefs.SetString("useUPNP", UseUPNP.isOn.ToString());
		
		UseUPNP.interactable = portOpener.UpnpWorks;
		if(portOpener.Checked) CancelInvoke("UseUpnpChange");
	}
}
