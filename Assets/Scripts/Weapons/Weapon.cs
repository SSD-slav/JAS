// Weapon
using System.Collections;
using Fragsurf.Movement;
using Unity.Netcode;
using UnityEngine;

public class Weapon : NetworkBehaviour
{
	public NetworkObject bullet;

	public NetworkVariable<float> ammo = new NetworkVariable<float>(NetworkVariableReadPermission.OwnerOnly);

	public float MaxAmmo;

	public float Damage;

	public float EXPLRadius;

	public float EXPLPower;

	public float ReloadTime;

	public float Delay;

	public float recoil;

	public float BulletSpped;

	public bool CanShoot = true;

	public virtual void Shot(Vector3 position, Vector3 dir, bool IsDir)
	{
		if (!CanShoot)
		{
			return;
		}
		if (ammo.Value < 1f)
		{
			Reload();
			return;
		}
		CanShoot = false;
		StartCoroutine(delay(Delay));
		ammo.Value--;
		NetworkObject networkObject = Instantiate(bullet);
		networkObject.transform.position = position;
		if (IsDir)
		{
			networkObject.transform.rotation = Quaternion.LookRotation(dir);
		}
		else
		{
			networkObject.transform.LookAt(dir, Vector3.up);
		}
		networkObject.SpawnWithOwnership(base.NetworkManager.ServerClientId);
		networkObject.GetComponent<Rigidbody>().AddForce(networkObject.transform.forward * BulletSpped, ForceMode.VelocityChange);
		Bullet component = networkObject.GetComponent<Bullet>();
		component.Dammage = Damage;
		component.EXPLRadius = EXPLRadius;
		component.EXPLPower = EXPLPower;
		MoveClientRpc(base.gameObject.GetComponentInParent<NetworkObject>().OwnerClientId, -networkObject.transform.forward * recoil);
	}

	[ClientRpc]
	private void MoveClientRpc(ulong go, Vector3 vel)
	{
		NetworkManager.ConnectedClients[go].PlayerObject.GetComponentInParent<SurfCharacter>().moveData.velocity += vel;
	}

	public virtual void Reload()
	{
		if (MaxAmmo > ammo.Value)
		{
			CanShoot = false;
			StartCoroutine(delay(ReloadTime));
			ammo.Value = MaxAmmo;
		}
	}

	private IEnumerator delay(float Delay)
	{
		yield return new WaitForSeconds(Delay);
		CanShoot = true;
	}
}
