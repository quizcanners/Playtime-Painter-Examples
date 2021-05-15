using QuizCanners.Utils;
using UnityEngine;
using QuizCanners.Inspect;
using PlaytimePainter.CameraModules;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PlaytimePainter.Examples
{
    [ExecuteInEditMode]
    public class SkyController : MonoBehaviour, IPEGI {

        public Light directional;
        public MeshRenderer skeRenderer;

        private void FindComponents()
        {
            if (!skeRenderer)
                skeRenderer = GetComponent<MeshRenderer>();

            if (directional) return;

            var ls = FindObjectsOfType<Light>();

            for (var i = 0; i < ls.Length; i++)
                if (ls[i].type == LightType.Directional)
                {
                    directional = ls[i];
                    i = ls.Length;
                }
        }

        private void OnEnable()
        {
            FindComponents();
            skeRenderer.enabled = Application.isPlaying;
        }
        
        public float skyDynamics = 0.1f;

        private readonly ShaderProperty.VectorValue _sunDirectionProperty = new ShaderProperty.VectorValue("_SunDirection");
        private readonly ShaderProperty.ColorFloat4Value _directionalColorProperty = new ShaderProperty.ColorFloat4Value("_Directional");
        private readonly ShaderProperty.VectorValue _offsetProperty = new ShaderProperty.VectorValue("_Off");

        [SerializeField] private Camera _mainCam;

        private Camera MainCam
        {
            get
            {

                if (!_mainCam)
                    _mainCam = GetComponentInParent<Camera>();

                if (!_mainCam)
                    _mainCam = Camera.main;
                
                return _mainCam;
            }
        }
        
        public virtual void Update() {

            if (directional != null) {
                var v3 = directional.transform.rotation * Vector3.back;
                _sunDirectionProperty.GlobalValue = new Vector4(v3.x, v3.y, v3.z);
                _directionalColorProperty.GlobalValue = directional.color;
            }
            
            if (!MainCam) return;

            var pos = _mainCam.transform.position * skyDynamics;

            _offsetProperty.GlobalValue = new Vector4(pos.x, pos.z, 0f, 0f);
        }

        private void LateUpdate() => transform.rotation = Quaternion.identity;

        private int _inspectedStuff = -1;

        public void Inspect()
        {
            if (_inspectedStuff == -1)
            {
                pegi.toggleDefaultInspector(this).nl();

                "Main Cam".edit(90, ref _mainCam).nl();
                "Directional Light".edit(90, ref directional).nl();
                "Sky Renderer".edit(90, ref skeRenderer).nl();
                "Sky dinamics".edit(90, ref skyDynamics).nl();

                if (_mainCam)
                {
                    if (_mainCam.clearFlags == CameraClearFlags.Skybox)
                    {
                        "Skybox will hide procedural sky".writeWarning();
                        if ("Set to Black Color".Click())
                        {
                            _mainCam.clearFlags = CameraClearFlags.Color;
                            _mainCam.backgroundColor = Color.clear;
                        }
                    }
                }
            }

            pegi.nl();

        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(SkyController))] internal class SkyControllerDrawer : PEGI_Inspector_Mono<SkyController> { }
#endif

}