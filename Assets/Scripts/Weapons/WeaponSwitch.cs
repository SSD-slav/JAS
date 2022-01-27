// WeaponSwitch
using System;
using Unity.Netcode;
using UnityEngine;

public class WeaponSwitch : NetworkBehaviour
{
	[SerializeField]
	private GameObject WeaponsPerent;

	public LayerMask Mask;
	private Camera cam;

	private NetworkVariable<int> WeaponID = new NetworkVariable<int>(NetworkVariableReadPermission.OwnerOnly);

	private void Start()
	{
		cam = GetComponentInChildren<Camera>();
		WeaponID.OnValueChanged += OnValChanged;
	}

	private void OnValChanged(int old, int newVal)
	{
		for (int i = 0; i < WeaponsPerent.transform.childCount; i++)
		{
			WeaponsPerent.transform.GetChild(i).gameObject.SetActive(i == newVal);
		}
	}

	private void Update()
	{
		if (!IsOwner) return;
		
		if (Input.GetAxis("Mouse ScrollWheel") > 0f)
		{
			WeaponID.Value++;
			if (WeaponID.Value > WeaponsPerent.transform.childCount - 1)
			{
				WeaponID.Value = 0;
			}
		}
		else if (Input.GetAxis("Mouse ScrollWheel") < 0f)
		{
			WeaponID.Value--;
			if (WeaponID.Value < 0)
			{
				WeaponID.Value = WeaponsPerent.transform.childCount - 1;
			}
		}
		if (Input.GetKey(KeyCode.Mouse0))
		{
			if (!WeaponsPerent.transform.GetChild(WeaponID.Value).GetComponent<Weapon>().CanShoot)
			{
				return;
			}
			Ray ray = cam.ViewportPointToRay(new Vector2(0.5f, 0.5f));
			Vector3 position = WeaponsPerent.transform.GetChild(WeaponID.Value).GetChild(0).position;
			if (Physics.Raycast(ray, out var hitInfo, Mask))
			{
				ShotServerRpc(position, hitInfo.point, isDir: false);
			}
			else
			{
				ShotServerRpc(position, ray.direction, isDir: true);
			}
		}
		if (Input.GetKeyDown(KeyCode.R))
		{
			ReloadServerRpc();
		}
	}

	[ServerRpc]
	private void ShotServerRpc(Vector3 pos, Vector3 dir, bool isDir)
	{
		WeaponsPerent.transform.GetChild(WeaponID.Value).GetComponent<Weapon>().Shot(pos, dir, isDir);
	}

	[ServerRpc]
	private void ReloadServerRpc()
	{
		WeaponsPerent.transform.GetChild(WeaponID.Value).GetComponent<Weapon>().Reload();
	}
}
