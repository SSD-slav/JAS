// Player
using System.Collections.Generic;
using Unity.Netcode;
using TMPro;
using Unity.Collections;
using UnityEngine;

public class Player : NetworkBehaviour
{
	[SerializeField]
	private List<GameObject> switchLayer = new List<GameObject>();

	[SerializeField]
	private PlayerAiming pa;

	[SerializeField]
	private GameObject Colider;

	public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>(NetworkVariableReadPermission.Everyone);

	private void Start()
	{
		if (base.IsOwner)
		{
			ChangeNameServerRpc(GameObject.Find("Canvas").transform.GetChild(0).GetChild(2)
				.GetComponent<TMP_InputField>()
				.text);
			foreach (GameObject item in switchLayer)
			{
				item.layer = LayerMask.NameToLayer("Transperent");
			}
		}
		else
		{
			pa.enabled = false;
		}
	}
	[ServerRpc]
	void ChangeNameServerRpc(FixedString32Bytes name)
	{
		PlayerName.Value = name;
	}
}
