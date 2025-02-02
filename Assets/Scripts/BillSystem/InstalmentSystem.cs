﻿using UnityEngine;
using UnityEngine.UI;

public class InstalmentSystem : MonoBehaviour
    {
    public bool PayWithInstalments;
    public static InstalmentSystem instance;
    public Slider sliderPaymentSplitter;
    public Button ButtonCall;
    public Text instalRequestText;
    public int InstalmentsToPayIn;
    public bool ApplyingForInstallments;
    public bool EligibleForInstalments;

    void Start ( )
        {
        instance = this;
        ApplyingForInstallments = false;
        EligibleForInstalments = false;
        }

    public void DisableAfterInstalment ( )
        {
        ApplyingForInstallments = false;
        sliderPaymentSplitter.gameObject.SetActive ( false );
        }

    void Update ( )
        {
        InstalmentsToPayIn = ( int ) sliderPaymentSplitter.value;

        if ( Debt.instance.currentDebt >= 500 )
            {
            ButtonCall.gameObject.SetActive ( true );
            instalRequestText.gameObject.SetActive ( true );
            EligibleForInstalments = true;
            }

        else
            {
            ButtonCall.gameObject.SetActive ( false );
            instalRequestText.gameObject.SetActive ( false );
            sliderPaymentSplitter.gameObject.SetActive ( false );
            }

        if ( ApplyingForInstallments == true )
            {
            sliderPaymentSplitter.gameObject.SetActive ( true );
            sliderPaymentSplitter.interactable = true;
            PayWithInstalments = true;
            }

        else
            {
            sliderPaymentSplitter.gameObject.SetActive ( false );
            sliderPaymentSplitter.interactable = false;
            PayWithInstalments = false;
            }
        }

    public void OnClickCallButton ( )
        {
        ApplyingForInstallments = true;
        ButtonCall.gameObject.SetActive ( false );
        instalRequestText.gameObject.SetActive ( false );
        }
    }
