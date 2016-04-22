﻿using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class PauseGame : MonoBehaviour
    {
    private Button button;
    private Text buttonText;
    public Text tempInfo;
    public bool paused;
 
    private void Update()
        {
        if ( Time.timeScale == 0.0f)
            {
            tempInfo.text = "Druk op Start om te beginnen!";
            }
        }

    private void Start ( )
        {
        GlobalAudio.instance.SoundPause ( );

        button = GetComponent<Button> ( );
        buttonText = button.GetComponentInChildren<Text> ( );
        tempInfo.text = "Druk op Start om te beginnen!";
        paused = false;
        Time.timeScale = 0.0f;
        buttonText.text = "Start!";

        }


    public void Paused ( )
        {
        paused = !paused;
        if ( paused )
            {
            tempInfo.text = "";
            Time.timeScale = 1.0f;
            buttonText.text = "Pauze";
            GlobalAudio.instance.SoundStart ( );
            }
        if ( !paused )
            {
            buttonText.text = "Start!";
            Time.timeScale = 0.0F;
            GlobalAudio.instance.SoundPause ( );
            }
        }
    }
 