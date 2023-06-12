using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace CommonCore.TurnBasedBattleSystem
{
    /// <summary>
    /// Controller for the TBBS UI
    /// </summary>
    public class TBBSUIController : MonoBehaviour
    {
        [Header("References"), SerializeField]
        private TBBSSceneController SceneController = null;


        public void PromptPlayerAndGetActions(Action callback)
        {
            //TODO open UI, let player select all the actions, then call the callback to return control back to scene controller
        }

        public void ShowMessage(string message) //TODO more args?
        {

        }
    }
}