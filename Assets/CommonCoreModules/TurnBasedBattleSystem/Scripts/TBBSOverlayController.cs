using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CommonCore.TurnBasedBattleSystem
{
    public class TBBSOverlayController : MonoBehaviour
    {
        [Header("Elements")]
        public CanvasGroup CanvasGroup;
        public Text NameText;
        public Slider HealthSlider;
        public Slider EnergySlider;
        public Transform ConditionsArea;

        [Header("Options")]
        public float YOffset;

        //script backing variables
        public string ParticipantName;
    }
}

