using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PrintSpeed : MonoBehaviour
{
    private TMP_Text text;

    private void Start()
    {
        text = GetComponent<TMP_Text>();
    }

    private SurfCharacter Character;
    private void Update()
    {
        if (Character == null)
        {
            Character = FindObjectOfType<SurfCharacter>();
        }
        else
        {
            text.text = "Speed: " + new Vector3(Character.baseVelocity.x, 0, Character.baseVelocity.z).magnitude + "\n" + Character.baseVelocity; 
        }
    }
}
