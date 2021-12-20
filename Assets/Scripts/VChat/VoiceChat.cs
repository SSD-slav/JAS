// VoiceChat
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class VoiceChat : NetworkBehaviour
{
	private int MaxFrequency;

	private int id;

	private string device;

	public AudioClip clip;

	public AudioSource source;

	private bool RunMic;

	private int LastSample;

	private AudioClip clipMicrophone;

	private CustomMessagingManager CMM;
	private void Start()
	{
		CMM = NetworkManager.CustomMessagingManager;
		if (base.IsLocalPlayer)
		{
			device = Microphone.devices[id];
			Microphone.GetDeviceCaps(device, out var _, out MaxFrequency);
			clipMicrophone = Microphone.Start(device, loop: true, 1, MaxFrequency);
			source.clip = clip;
			CMM.RegisterNamedMessageHandler("VoiceChatToClient", ResiveSound);
			CMM.RegisterNamedMessageHandler("VoiceChatToServer", ServerReceiveAndSend);
			source.loop = false;
		}
	}

	private void ServerReceiveAndSend(ulong senderClientId, FastBufferReader reader)
	{
		FastBufferWriter writer = new FastBufferWriter();
		int data;
		reader.ReadValueSafe(out data);
		float[] data2;
		reader.ReadValueSafe(out data2);
		writer.WriteValueSafe(data);
		writer.WriteValueSafe(data2);
		List<ulong> list = base.NetworkManager.ConnectedClients.Keys.ToList();
		list.Remove(senderClientId);
		CMM.SendNamedMessage("VoiceChatToClient", list, writer);
		ResiveSound(senderClientId, reader);
	}

	private void Update()
	{
		if (!base.IsLocalPlayer)
		{
			return;
		}
		float[] array = LastSoundData();
		if (array.Length < 1)
		{
			return;
		}
		if (!Input.GetKeyDown(KeyCode.E))
		{
			RunMic = !RunMic;
		}
		if (RunMic)
		{
			FastBufferWriter writer = new FastBufferWriter();
			writer.WriteValueSafe(array.Length);
			writer.WriteValueSafe(array, array.Length);
			if (IsHost || IsServer)
			{
				List<ulong> list = base.NetworkManager.ConnectedClients.Keys.ToList();
				list.Remove(base.NetworkManager.ServerClientId);
				CMM.SendNamedMessage("VoiceChatToClient", list, writer);
				VoiceChat component = base.NetworkManager.ConnectedClients[NetworkManager.ServerClientId].PlayerObject.GetComponent<VoiceChat>();
				component.clip = AudioClip.Create("clip", array.Length, 1, MaxFrequency, stream: false);
				component.clip.SetData(array, 0);
				component.CancelInvoke("StopLoop");
				component.source.clip = clip;
				source.loop = true;
				component.source.Play();
				component.Invoke("StopLoop", 0.1f);
			}
			else
			{
				CMM.SendNamedMessage("VoiceChatToServer", base.NetworkManager.ServerClientId, writer);
			}
		}
	}

	private void StopLoop()
	{
		source.loop = false;
	}

	private void ResiveSound(ulong senderClientId, FastBufferReader reader)
	{
		int num;
		reader.ReadValueSafe(out num);
		float[] array = new float[num];
		reader.ReadValueSafe(out array);
		VoiceChat component = base.NetworkManager.ConnectedClients[senderClientId].PlayerObject.GetComponent<VoiceChat>();
		component.clip = AudioClip.Create("clip", array.Length, 1, MaxFrequency, stream: false);
		component.clip.SetData(array, 0);
		component.CancelInvoke("StopLoop");
		component.source.clip = clip;
		source.loop = true;
		component.source.Play();
		component.Invoke("StopLoop", 0.1f);
	}

	private float[] LastSoundData()
	{
		if (clipMicrophone == null)
		{
			clipMicrophone = Microphone.Start(device, loop: true, 1, MaxFrequency);
		}
		int position = Microphone.GetPosition(device);
		if (LastSample > position)
		{
			LastSample = 0;
		}
		float[] array = new float[clipMicrophone.samples * clipMicrophone.channels];
		clipMicrophone.GetData(array, 0);
		float[] array2 = new float[(position - LastSample) / clipMicrophone.channels];
		for (int i = LastSample; i < position; i += clipMicrophone.channels)
		{
			array2[i - LastSample] = array[i];
		}
		LastSample = position;
		return array2;
	}
}
