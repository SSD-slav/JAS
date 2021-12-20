// DelMeAfterTime
using UnityEngine;

public class DelMeAfterTime : MonoBehaviour
{
	public void StartDel(float f)
	{
		Invoke("Del", f);
	}

	private void Del()
	{
		Object.Destroy(base.gameObject);
	}
}
