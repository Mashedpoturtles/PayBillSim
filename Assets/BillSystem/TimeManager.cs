﻿using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;

public class TimeManager : MonoBehaviour {

    public Text timeToTextUI;
    public static TimeManager instance;
    public static DateTime currentTime;
    public static DateTime duedateelect;
 
    void Start()
    {
        currentTime = new DateTime(2016, 1, 1);
        StartCoroutine(AddHours());
        instance = this;
        duedateelect = new DateTime(2016, 1, 1);
    }

    void Update()
    {
        Timer();
    }

    //Hours are seconds and time passes in 24 hour cycles.
    public void Timer()
    { 
        currentTime = currentTime.AddHours(1 * Time.deltaTime);
        timeToTextUI.text = timeToTextUI.text = currentTime.DayOfWeek.ToString() + currentTime.ToString(" MMMM , yyyy ") + "Current Time: " +currentTime.ToString("HH: tt") + ".";
    }
    [SerializeField]
    float speedUp = 1.0f;
    IEnumerator AddHours()
    {
        while (true)
        {
            currentTime = currentTime.AddHours(1);
            yield return new WaitForSeconds(1.0f / speedUp);
        }
    }
}
