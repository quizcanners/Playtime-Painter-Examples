using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;

namespace PainterTool.Examples
{

    [ExecuteInEditMode]
    public class HintController : MonoBehaviour, IPEGI
    {
        [SerializeField] private GameObject _picture;
        [SerializeField] private GameObject _pill;
        [SerializeField] private GameObject _cube;
        private readonly Gate.SystemTime _timer = new Gate.SystemTime(Gate.InitialValue.Uninitialized);

        private string _text = "";

        private HintStage _stage;
        private enum HintStage { EnableTool, Draw, AddTool, AddTexture, RenderTexture, WellDone }

        private PainterComponent pp;
        private PainterComponent PillPainter
        {
            get
            {
                if (!pp)
                    pp = _pill.GetComponent<PainterComponent>();

                return pp;
            }
        }

        private void SetStage(HintStage st)
        {
            _stage = st;

            string newText;

            switch (_stage)
            {
                case HintStage.EnableTool:

                    if (Application.isEditor == false)
                    {
                        newText = "Tutorial is Designed to be played in Editor";
                    } else 
                    {
                        newText = "Select any object with PlaytimePainter Component and click 'On/Off' icon to start using painter." +
                            (Application.isPlaying ? "" : "Alternatively select the tool in Unity's toolbox (a dropdown next where all the transform tools are)");
                    }

                        ; break;
                case HintStage.Draw:
                    _timer.GetSecondsDeltaAndUpdate();
                    newText = "Draw on the {0} in {1} view. ".F(_cube.name, Application.isPlaying ? "Game View" : "Scene View"); break;
                case HintStage.AddTool:
                    var cmp = _picture.GetComponent<PainterComponent>();
                    if (cmp)
                        cmp.DestroyWhateverComponent();

                 

                    newText = "{0} to the right has no tool attached. \n Select it and \n Click 'Add Component'->'Mesh'->'Playtime Painter'".F(_picture);
                    break;

                   

                case HintStage.AddTexture:

                    if (PillPainter) 
                        PillPainter.ChangeTexture(null);


                    newText = "Pill on the left has no texture. Select it with and click 'Create Texture' icon"; break;
                case HintStage.RenderTexture:
                    int size = RenderTextureBuffersManager.renderBuffersSize;
                    newText = "Change MODE to GPU Blit and paint with it. \n This will enable more options, improve performance and will use two " + size + "*" + size +
                              " Render Texture buffers for editing. \n"; break;
                case HintStage.WellDone: goto default;
                default:
                    newText = "Well Done! Remember to save your textures before entering/exiting Play Mode or closing Unity. And use Preview shader to see selected texture and stroke size.";
                    break;
            }

            _text = newText;

        }

        private void OnEnable()
        {
            SetStage(HintStage.EnableTool);
            tooltipStyle.wordWrap = true;
        }

        public GUIStyle tooltipStyle = new GUIStyle();

        private void OnGUI()
        {
            var cont = new GUIContent(_text);
            GUI.Box(new Rect(Screen.width - 400, 10, 390, 100), cont, tooltipStyle);
        }

        private void Update()
        {
            if (!PainterComponent.IsCurrentTool)
                SetStage(HintStage.EnableTool);

            switch (_stage)
            {
                case HintStage.EnableTool:
                    if (PainterComponent.IsCurrentTool)
                    {
                        SetStage(HintStage.Draw);
                    }
                    break;
                case HintStage.Draw:

                    if (_timer.TryUpdateIfTimePassed(secondsPassed: 5))
                    {
                         SetStage(HintStage.AddTool);
                    }
                    break;
                case HintStage.AddTool:
                    if (_picture.GetComponent<PainterComponent>()) { SetStage(HintStage.AddTexture); }
                    break;
                case HintStage.AddTexture:
                    if (PillPainter && PillPainter.TexMeta != null) SetStage(HintStage.RenderTexture); break;
                case HintStage.RenderTexture:

                    var pntr = PainterComponent.currentlyPaintedObjectPainter;

                    if (pntr && pntr.TexMeta != null && pntr.TexMeta.TargetIsRenderTexture())
                        SetStage(HintStage.WellDone); break;
            }
        }

        void IPEGI.Inspect()
        {
            if ("Restart Tutorial".PegiLabel().Click().Nl()) 
            {
                PainterComponent.IsCurrentTool = false;
            }

        }
    }

    [PEGI_Inspector_Override(typeof(HintController))] internal class HintControllerDrawer : PEGI_Inspector_Override { }
}