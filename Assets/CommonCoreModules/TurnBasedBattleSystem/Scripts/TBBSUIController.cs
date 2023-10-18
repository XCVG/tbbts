using CommonCore.Util;
using CommonCore.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using CommonCore.World;
using static UnityEngine.GraphicsBuffer;

namespace CommonCore.TurnBasedBattleSystem
{
    /// <summary>
    /// Controller for the TBBS UI
    /// </summary>
    public class TBBSUIController : MonoBehaviour
    {
        [Header("References"), SerializeField]
        private TBBSSceneController SceneController = null;
        [SerializeField]
        private Canvas UICanvas = null;

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

        [Header("Pick Target"), SerializeField]
        private GameObject PickTargetContainer = null;
        [SerializeField]
        private GameObject PickTargetInstructionPanel = null;
        [SerializeField]
        private Text PickTargetInstructionText = null;
        [SerializeField]
        private GameObject PickTargetButtonTemplate = null;

        [Header("Overlay"), SerializeField]
        private GameObject OverlayContainer = null;

        [Header("Theming"), SerializeField]
        private bool EnableTheming = true;
        [SerializeField]
        private string ThemeOverride = null;


        private Action ActionSelectDoneCallback;

        private void Start()
        {
            if (UICanvas == null)
                UICanvas = GetComponent<Canvas>();

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
            //WIP this will be more complicated because it will be per-character
            Debug.LogWarning("PresentActionSelect2");            

            //yeah we'll just do a massive LINQ query every turn, it's FINE
            var playerControlledParticipants = SceneController
                .ParticipantData
                .Where(kvp => kvp.Value.BattleParticipant.ControlledBy == BattleParticipant.ControlledByType.Player)
                .Where(kvp => kvp.Value.Health > 0)
                .Where(kvp => !kvp.Value.Conditions.Any(c => c is TBBSConditionBase tc && tc.BlockActions))
                .ToList();
            var targetableParticipants = SceneController
                .ParticipantData
                .Where(kvp => kvp.Value.BattleParticipant.ControlledBy == BattleParticipant.ControlledByType.AI)
                .Where(kvp => kvp.Value.Health > 0)
                .Where(kvp => !kvp.Value.Conditions.Any(c => c is TBBSConditionBase tc && tc.BlockTargeting))
                .ToList();
            int participantIndex = 0;
            presentActionSelectForParticipant();

            void presentActionSelectForParticipant()
            {
                ActionSelect2Panel.SetActive(true);
                var participant = playerControlledParticipants[participantIndex];
                ParticipantNameText.text = participant.Value.DisplayName;

                AttackButton.onClick.RemoveAllListeners();
                if (targetableParticipants.Count == 0)
                {
                    AttackButton.interactable = false;
                }
                else
                {
                    AttackButton.interactable = true;
                    AttackButton.onClick.AddListener(() =>
                    {
                        if (targetableParticipants.Count == 1)
                        {
                            SceneController.ActionQueue.Add(new SimpleAttackAction() { AttackingParticipant = participant.Key, DefendingParticipant = targetableParticipants[0].Key, Move = "Attack", AttackPriority = (int)participant.Value.Stats[TBBSStatType.Agility] });
                            gotoNext();
                        }
                        else
                        {
                            PickTarget(targetableParticipants, (target) =>
                            {
                                SceneController.ActionQueue.Add(new SimpleAttackAction() { AttackingParticipant = participant.Key, DefendingParticipant = target, Move = "Attack", AttackPriority = (int)participant.Value.Stats[TBBSStatType.Agility] });
                                gotoNext();
                            });
                        }

                        
                    });
                }
                

                GuardButton.onClick.RemoveAllListeners();
                GuardButton.onClick.AddListener(() =>
                {
                    SceneController.ActionQueue.Add(new GuardAction() { GuardingParticipant = participant.Key }); //don't need to worry about agility or anything because Guard actions will be reordered ahead of attacks
                    gotoNext();
                });

                EventSystem.current.SetSelectedGameObject(AttackButton.gameObject);
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

        private void PickTarget(IEnumerable<KeyValuePair<string, ParticipantData>> targetableParticipants, Action<string> callback)
        {
            //hide overlay if showing
            HideOverlay();

            ActionSelect2Panel.SetActive(false);

            PickTargetInstructionPanel.SetActive(true);
            PickTargetContainer.SetActive(true);
            PickTargetContainer.transform.DestroyAllChildren();

            var camera = WorldUtils.GetActiveCamera();
            var canvasTransform = UICanvas.transform as RectTransform;

            List<GameObject> buttonObjs = new List<GameObject>();
            foreach (var target in targetableParticipants)
            {
                string targetName = target.Key;
                var battler = GetBattlerForParticipant(targetName);

                var newButtonObj = GameObject.Instantiate(PickTargetButtonTemplate, PickTargetContainer.transform);
                var newButtonTransform = newButtonObj.GetComponent<RectTransform>();
                var newButton = newButtonObj.GetComponent<Button>();
                newButton.onClick.AddListener(() => targetPicked(targetName));

                //should probably pull this upstream into utils
                Vector2 vpPos = camera.WorldToViewportPoint(battler.GetOverlayPoint());
                Vector2 screenPos = new Vector2(((vpPos.x * canvasTransform.sizeDelta.x) - (canvasTransform.sizeDelta.x * 0.5f)), ((vpPos.y * canvasTransform.sizeDelta.y) - (canvasTransform.sizeDelta.y * 0.5f)));
                newButtonTransform.anchoredPosition = screenPos;

                buttonObjs.Add(newButtonObj);
            }

            ApplyThemeTo(PickTargetContainer.transform);
            EventSystem.current.SetSelectedGameObject(buttonObjs[0]);

            void targetPicked (string targetName)
            {
                //clean up and return 

                PickTargetInstructionPanel.SetActive(false);
                PickTargetContainer.transform.DestroyAllChildren();

                callback(targetName);
            }

            //TODO abort handling, styling, etc

        }

        public IEnumerator ShowMessageAndWait(string message)
        {
            bool advanced = false;
            ShowMessage(message, () =>
            {
                advanced = true;
            });
            while (!advanced)
            {
                yield return null;
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

        public void HideOverlay()
        {
            OverlayContainer.SetActive(false);
        }

        public void ShowOverlay()
        {
            OverlayContainer.SetActive(true);
        }

        public void RepaintOverlay()
        {
            //TODO
        }

        private BattlerController GetBattlerForParticipant(string name)
        {
            if (SceneController.Battlers.TryGetValue(name, out var battlerController))
                return battlerController;

            Debug.LogWarning($"No battler found for participant \"{name}\"");

            return null;
        }

        private void ApplyThemeTo(Transform targetElement)
        {
            if (!EnableTheming)
                return;

            var uiModule = CCBase.GetModule<UIModule>();

            if (!string.IsNullOrEmpty(ThemeOverride))
            {
                uiModule.ApplyThemeRecurse(targetElement, uiModule.GetThemeByName(ThemeOverride));
            }
            else
            {
                uiModule.ApplyThemeRecurse(targetElement);
            }
        }


    }
}