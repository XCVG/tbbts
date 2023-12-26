using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CommonCore.TurnBasedBattleSystem
{
    public class TBBSStageController : MonoBehaviour
    {
        [Header("References"), SerializeField]
        private TBBSSceneController SceneController = null;

        [Header("Lighting"), SerializeField]
        private bool SetSkybox = true;
        [SerializeField]
        private Material Skybox = null;
        [SerializeField]
        private bool SetLighting = false;
        [SerializeField]
        private Color LightColor = Color.white;

        //TODO if we need it, transform to copy for camera object 

        public void Init(TBBSSceneController sceneController)
        {
            SceneController = sceneController;
            SetupLightingEnvironment();
        }

        private void SetupLightingEnvironment()
        {
            if (SetSkybox && Skybox != null)
            {
                RenderSettings.skybox = Skybox;
            }

            if (SetLighting)
            {
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = LightColor;
            }
        }
    }
}


