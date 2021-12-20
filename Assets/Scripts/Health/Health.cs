// Health
using Unity.Netcode;
using UnityEngine;

public class Health : NetworkBehaviour
{
	private NetworkVariable<float> HealthPoints = new NetworkVariable<float>(NetworkVariableReadPermission.OwnerOnly, 100);

	private float MaxHealth = 100f;

	private GameObject dead;

	private void Start()
	{
		if (IsOwner)
		{
			dead = GameObject.Find("lolurdead");
			dead.SetActive(value: false);
		}
	}

	public void AddHp(float HP)
	{
		AddHPServerRpc(HP);
	}

	[ServerRpc(RequireOwnership = false)]
	private void AddHPServerRpc(float HP)
	{
		if (IsServer || IsHost)
		{
			HealthPoints.Value += HP;
			if (HealthPoints.Value > MaxHealth)
			{
				HealthPoints.Value = MaxHealth;
			}
			if (HealthPoints.Value < 0f)
			{
				DieClientRpc();
			}
		}
	}

	[ClientRpc]
	private void DieClientRpc()
	{
		if ((IsClient || IsHost) && IsOwner)
		{
			dead.SetActive(value: true);
		}
	}
}
