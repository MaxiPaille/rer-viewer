using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class Clock : MonoBehaviour 
{

    private Text _text;

    void Start()
    {
        _text = GetComponent<Text>();
    }

	void Update ()
    {
        _text.text = String.Format("{0:00}", DateTime.Now.Hour) + ":" + String.Format("{0:00}", DateTime.Now.Minute);
	}
}
