using Fragsurf.Movement;
using Unity.Netcode;
using TMPro;
using Unity.Collections;
using UnityEngine;

public class Chat : NetworkBehaviour
{
  
  [SerializeField] private TMP_InputField InputField;
  [SerializeField] private GameObject Content;
  [SerializeField] private GameObject messagePrefab;
  private bool isLocked = true;
  private CustomMessagingManager CMM;

  private void Start()
  {
    if (!IsOwner) enabled = false;

    CMM = NetworkManager.CustomMessagingManager;
    if (IsServer || IsHost)
    {
      CMM.RegisterNamedMessageHandler("ChatSendToServer", ServerSend);
    }
    CMM.RegisterNamedMessageHandler("ChatSendToClient", ServerSend);

    Content = GameObject.Find("ContentChat");
    InputField = GameObject.Find("InputFieldChat").GetComponent<TMP_InputField>();
  }


  private void Update()
  {
    if (Input.GetKeyDown(KeyCode.Escape))
    {
      Cursor.visible = isLocked;
      isLocked = !isLocked;
      gameObject.GetComponent<SurfCharacter>().enabled = this.isLocked;
      gameObject.transform.GetChild(1).GetComponent<PlayerAiming>().enabled = this.isLocked;
      if (isLocked)
      {
        Cursor.lockState = CursorLockMode.Locked;
        InputField.DeactivateInputField(true);
      }
      else
      {
        Cursor.lockState = CursorLockMode.None;
        InputField.ActivateInputField();
        InputField.Select();
      }
    }

    if (!Input.GetKeyDown(KeyCode.Return) || string.IsNullOrEmpty(InputField.text))
      return;
    AddText("You: " + InputField.text);
    SendToAll(InputField.text);
    if (IsHost || IsServer)
    {
      ReceiveMessageClientRpc(NetworkManager.ServerClientId,
        NetworkManager.ConnectedClients[NetworkManager.ServerClientId].PlayerObject.GetComponent<Player>()
          .PlayerName.Value + ": " + InputField.text);
    }

    Cursor.visible = isLocked;
    isLocked = !isLocked;
    gameObject.transform.GetChild(1).GetComponent<PlayerAiming>().enabled = this.isLocked;
    gameObject.GetComponent<SurfCharacter>().enabled = isLocked;
    Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
    InputField.DeactivateInputField(true);
    InputField.text = "";
  }

  private void AddText(string text)
  {
    Instantiate(messagePrefab, Content.transform).GetComponent<TMP_Text>().text = text;
    if (Content.transform.childCount <= 501) return;

    Destroy(Content.transform.GetChild(0));
    Content.transform.GetChild(1).GetComponent<TMP_Text>().text = "<color = red><b>Chat size exidse 500</b></color>";
  }

  [ClientRpc]
  private void ReceiveMessageClientRpc(ulong senderClientId, string data)
  {
    if (senderClientId == NetworkManager.LocalClientId)
      return;
    
    AddText(data);
  }

  private void ServerSend(ulong senderclientid, FastBufferReader messagepayload)
  {
    string str1;
    messagepayload.ReadValueSafe(out str1);
    string str2 = NetworkManager.ConnectedClients[senderclientid].PlayerObject.GetComponent<Player>().PlayerName.Value.Value;
    ReceiveMessageClientRpc(senderclientid, str2 + ": " + str1);
  }

  public void SendToAll(string message)
  {
    using FastBufferWriter writer = new FastBufferWriter();
    writer.WriteValueSafe(message);
    CMM.SendNamedMessage("ChatSendToServer", NetworkManager.ServerClientId, writer);
    writer.Dispose();
  }
  
}