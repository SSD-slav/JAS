// NetworkDiscovery
using System;
using System.Net;
using Unity.Netcode;
using Unity.Netcode.Transports.UNET;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(NetworkManager))]
public class NetworkDiscovery : NetworkDiscovery<DiscoveryBroadcastData, DiscoveryResponseData>
{
	[Serializable]
	public class ServerFoundEvent : UnityEvent<IPEndPoint, DiscoveryResponseData>
	{
	}

	private NetworkManager m_NetworkManager;

	[SerializeField]
	[Tooltip("If true NetworkDiscovery will make the server visible and answer to client broadcasts as soon as MLAPI starts running as server.")]
	private bool m_StartWithServer = true;

	public string ServerName = "EnterName";

	public ServerFoundEvent OnServerFound;

	private bool m_HasStartedWithServer;

	public void Awake()
	{
		m_NetworkManager = GetComponent<NetworkManager>();
	}

	public void Update()
	{
		if (m_StartWithServer && !m_HasStartedWithServer && !base.IsRunning && m_NetworkManager.IsServer)
		{
			StartServer();
			m_HasStartedWithServer = true;
		}
	}

	protected override bool ProcessBroadcast(IPEndPoint sender, DiscoveryBroadcastData broadCast, out DiscoveryResponseData response)
	{
		response = new DiscoveryResponseData
		{
			ServerName = ServerName,
			Port = (ushort)((UNetTransport)m_NetworkManager.NetworkConfig.NetworkTransport).ConnectPort
		};
		return true;
	}

	protected override void ResponseReceived(IPEndPoint sender, DiscoveryResponseData response)
	{
		OnServerFound.Invoke(sender, response);
	}
}
