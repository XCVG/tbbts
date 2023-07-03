using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CommonCore.TurnBasedBattleSystem
{
    /// <summary>
    /// Controller for the TBBS UI
    /// </summary>
    public class TBBSUIController : MonoBehaviour
    {
        [Header("References"), SerializeField]
        private TBBSSceneController SceneController = null;

        [Header("Message Panel"), SerializeField]
        private GameObject MessagePanel = null;
        [SerializeField]
        private Text MessageText = null;
        [SerializeField]
        private Button MessageAdvanceButton = null;

        [Header("Action Select 1 Panel (Fight/Flight)"), SerializeField]
        private GameObject ActionSelect1Panel = null;
        [SerializeField]
        private Button FightButton = null;
        [SerializeField]
        private Button FlightButton = null;

        [Header("Action Select 2 Panel (Move Selection)"), SerializeField]
        private GameObject ActionSelect2Panel = null;
        [SerializeField]
        private Text ParticipantNameText = null;
        [SerializeField]
        private Button AttackButton = null;
        [SerializeField]
        private Button GuardButton = null;

        private Action ActionSelectDoneCallback;

        private void Start()
        {
            //TODO theming
        }


        public void PromptPlayerAndGetActions(Action callback)
        {
            //TODO open UI, let player select all the actions, then call the callback to return control back to scene controller
            ActionSelectDoneCallback = callback;
            PresentActionSelect1();
        }

        private void PresentActionSelect1()
        {
            ActionSelect1Panel.SetActive(true);

            
            //this is not ideal, it's like going $button.off('click').on('click') every time but IT'S FINE
            FightButton.onClick.RemoveAllListeners();
            FightButton.onClick.AddListener(() =>
            {
                //continue to step 2
                ActionSelect1Panel.SetActive(false);
                PresentActionSelect2();
            });

            //TODO show/don't show flee action based on battle data

            FlightButton.onClick.RemoveAllListeners();
            FlightButton.onClick.AddListener(() =>
            {
                //push Flee action to queue, close panel, and return control back to scene controller
                SceneController.ActionQueue.Add(new FleeAction());
                ActionSelect1Panel.SetActive(false);
                ActionSelectDoneCallback();
            });

            EventSystem.current.SetSelectedGameObject(FightButton.gameObject);
        }

        private void PresentActionSelect2()
        {
            //TODO this will be more complicated because it will be per-character
            Debug.LogWarning("PresentActionSelect2");

            ActionSelect2Panel.SetActive(true);

            //do we need to do this every time? not really
            var playerControlledParticipants = SceneController
                .ParticipantData
                .Where(kvp => kvp.Value.BattleParticipant.ControlledBy == BattleParticipant.ControlledByType.Player)
                .Where(kvp => kvp.Value.Health > 0) //temporary
                .ToList();
            //TODO need to filter by more condition or flag for player-controlled participants that can't do anything this turn because they're paralyzed or dead or whatever
            int participantIndex = 0;
            presentActionSelectForParticipant();

            void presentActionSelectForParticipant()
            {
                var participant = playerControlledParticipants[participantIndex];
                ParticipantNameText.text = participant.Value.DisplayName;

                AttackButton.onClick.RemoveAllListeners();
                AttackButton.onClick.AddListener(() =>
                {
                    //TODO this will be complicated because you need to select a target
                    //probably need to handle agility better
                    //TODO
                    throw new NotImplementedException();
                    gotoNext();
                });

                GuardButton.onClick.RemoveAllListeners();
                GuardButton.onClick.AddListener(() =>
                {
                    SceneController.ActionQueue.Add(new GuardAction()); //don't need to worry about agility or anything because Guard actions will be reordered ahead of attacks
                    gotoNext();
                });
            }

            void gotoNext()
            {
                participantIndex++;
                if(participantIndex < playerControlledParticipants.Count)
                {
                    presentActionSelectForParticipant();
                }
                else
                {
                    ActionSelect2Panel.SetActive(false);
                    ActionSelectDoneCallback();
                }
            }
        }

        public void ShowMessage(string message) => ShowMessage(message, null);

        public void ShowMessage(string message, Action callback) //TODO more args?
        {
            MessageText.text = message;
            MessagePanel.SetActive(true);
            if(callback != null)
            {
                //setup button
                MessageAdvanceButton.gameObject.SetActive(true);
                MessageAdvanceButton.onClick.RemoveAllListeners();
                MessageAdvanceButton.onClick.AddListener(() =>
                {
                    ClearMessage();
                    callback();                    
                });
                EventSystem.current.SetSelectedGameObject(MessageAdvanceButton.gameObject);
            }
            else
            {
                MessageAdvanceButton.gameObject.SetActive(false);
            }
        }

        public void ClearMessage()
        {
            MessageText.text = String.Empty;
            MessagePanel.SetActive(false);
        }

        //TODO overlay show/hide/update methods

    }
}