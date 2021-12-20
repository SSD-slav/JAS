// Bullet
using Fragsurf.Movement;
using Unity.Netcode;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
	public GameObject EXPL_Pref;

	public float Dammage = 100f;

	public float EXPLRadius;

	public float EXPLPower;

	private void Start()
	{
		Invoke("TimeOut", 20f);
	}

	private void TimeOut()
	{
		if (IsServer)
		{
			Destroy(gameObject);
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		OnHit(collision);
	}

	public virtual void OnHit(Collision collision)
	{
		if(!IsServer) return;
		
		if (collision.gameObject.tag == "Damageable")
		{
			collision.gameObject.GetComponent<Health>().AddHp(0f - Dammage);
		}

		Expl(collision.collider.ClosestPoint(transform.position), EXPLRadius, EXPLPower);
		ExplClientRpc(collision.collider.ClosestPoint(transform.position), EXPLRadius, EXPLPower);
		Destroy(gameObject);
	}

	[ClientRpc]
	private void ExplClientRpc(Vector3 pos, float Radius, float Power)
	{
		Expl(pos, Radius, Power);
	}
	
	private void Expl(Vector3 pos, float Radius, float Power)
	{
		if (Radius == 0f)
		{
			return;
		}
		Collider[] array = Physics.OverlapSphere(pos, Radius);
		foreach (Collider collider in array)
		{
			if (collider.gameObject.layer == LayerMask.NameToLayer("Player"))
			{
				SurfCharacter component = null;
				collider.transform.parent.gameObject.TryGetComponent<SurfCharacter>(out component);
				if (component != null)
				{
					component.moveData.velocity += (collider.transform.position - collider.ClosestPoint(transform.position)) * 1f / Vector3.Distance(collider.transform.position, transform.position) * Power;
					collider.gameObject.GetComponent<Health>().AddHp(0f - 1f / Vector3.Distance(collider.transform.position, transform.position) * Power);
				}
			}
		}
		GameObject obj = Instantiate(EXPL_Pref, transform.position, Quaternion.identity);
		obj.AddComponent<DelMeAfterTime>().StartDel(1f);
		obj.transform.localScale = Radius * Vector3.one;
	}
}
