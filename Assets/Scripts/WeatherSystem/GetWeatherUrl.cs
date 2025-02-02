﻿using UnityEngine;
using System.Collections.Generic;
using MovementEffects;
using UnityEngine.UI;
using Newtonsoft.Json;


public class GetWeatherUrl : MonoBehaviour
    {
    private static string json;
    public static double dTemp;
    public static double dFeelTempC;
    public static string sWeatherCondition;
    public string sCountry;
    public string sCity;
    protected string newsfeed;
    [SerializeField]
    private GameObject gWeatherObject;
    public string url = "https://api.wunderground.com/api/d33ba8071e6bde8b/conditions/bestfct/q/autoip.json";
    public Text Weather;

    public IEnumerator<float> _ServerCall ( )
        {
        while ( true )
            {
            WWW www = new WWW ( url );

            yield return Timing.WaitUntilDone ( www );

            json = www.text;

            var MainObservation = JsonConvert.DeserializeObject<MainObservation> ( json );
            if ( MainObservation != null && MainObservation.current_observation != null )
                {
                for ( int i = 0 ; i < MainObservation.current_observation.weather.Length ; i++ )
                    dTemp = MainObservation.current_observation.temp_c;
                dFeelTempC = MainObservation.current_observation.feelslike_c;
                sWeatherCondition = MainObservation.current_observation.weather;
                sCountry = MainObservation.current_observation.display_location.state_name;
                sCity = MainObservation.current_observation.display_location.city;

                WeatherListComplete WeatherCompareScript = gWeatherObject.GetComponent<WeatherListComplete> ( );
                if ( WeatherCompareScript == null )
                    WeatherCompareScript = gWeatherObject.AddComponent<WeatherListComplete> ( );
                WeatherCompareScript.WeatherEffects ( );
                    {
                    Weather.text = string.Format ( "Temperatuur {0}\\n  Gevoels temperatuur {1}\\n Uw huidige locatie {2}\\n huidig weer: {3}"
                      , dTemp, dFeelTempC, sCity, sWeatherCondition ).Replace ( "\\n", "\n" );
                    }
                }
            yield break;
            }
        }

    public void Fetch ( )
        {
        Timing.RunCoroutine ( _ServerCall ( ) );
        }
    public void Start ( )
        {
        InvokeRepeating ( "Fetch", 1, 120 );
        }
    }
