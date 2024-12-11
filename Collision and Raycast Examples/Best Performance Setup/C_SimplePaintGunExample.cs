using System.Collections.Generic;
using System.Linq;
using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;

namespace PainterTool.Examples
{

    public class C_SimplePaintGunExample : MonoBehaviour, IPEGI
    {
        [SerializeField] private PaintingMode _mode;
        [SerializeField] private Brush _brush = new();
        [SerializeField] private int _shoots = 1;
        [SerializeField] private bool _continious;
        [SerializeField] private float _spread;

        private readonly Stroke continiousStroke = new(); // For continious
        private C_PaintingReceiver previousTargetForContinious;
      

        private enum PaintingMode { ShootInViewDirection, ShootInMousePointedDirection, Manual  }

        private void Update()
        {
            if (Input.GetMouseButton(0))
            {
                switch (_mode) {
                    case PaintingMode.ShootInViewDirection: Paint(transform.position + transform.forward); break;
                    case PaintingMode.ShootInMousePointedDirection:

                        var cam = Camera.main;
                        if (cam)
                        {
                            var ray = cam.ScreenPointToRay(Input.mousePosition);
                            if (Physics.Raycast(ray, out RaycastHit hit))
                                Paint(hit.point);
                        }
                        else
                            QcLog.ChillLogger.LogErrorOnce("No Main Camera", key: "NoMainCam", this);
                        break;
                }
            }
            else if (_continious)
                continiousStroke.OnStrokeEnd();
        }

        private static readonly List<TextureMeta> _texturesNeedUpdate = new();

        public void Paint(Vector3 targetPosition)
        {

            Vector3 direction = targetPosition - transform.position;

            for (var i = 0; i < (_continious ? 1 : _shoots); i++)
                if (Physics.Raycast(new Ray(origin: transform.position, direction: direction +
                                (_continious 
                                    ? Vector3.zero 
                                    : (transform.right * Random.Range(-_spread, _spread) + transform.up * Random.Range(-_spread, _spread)))
                                ), out RaycastHit hit)) 
                {
                    var receivers = hit.transform.GetComponentsInParent<C_PaintingReceiver>();

                    Debug.DrawLine(transform.position, hit.point, (receivers.Length == 0) ? Color.blue : Color.red, duration: 0.0f, depthTest: false);

                    if (receivers.Length == 0) 
                        continue;

                    C_PaintingReceiver receiver = receivers[0];

                    #region Multiple Submeshes
                    if (hit.TryGetSubMeshIndex_MAlloc(out int subMesh)) 
                    {
                        if (receivers.Length > 1)
                        {
                            var mats = receiver.Renderer.materials;

                            var material = mats[subMesh % mats.Length];

                            receiver = receivers.FirstOrDefault(r => r.CurrentMaterial == material);
                        }
                    }
                    else
                        subMesh = receiver.materialIndex;

                    #endregion

                    if (!receiver) 
                        continue;

                    var tex = receiver.GetTexture();

                    if (!tex) 
                        continue;

                    var rendTex = tex as RenderTexture;

                    #region  WORLD SPACE BRUSH

                    if (_continious)
                    {
                        if (previousTargetForContinious && (receiver != previousTargetForContinious))
                            continiousStroke.OnStrokeEnd();

                        previousTargetForContinious = receiver;
                    }

                    if (rendTex)
                    {
                        var st = _continious ? continiousStroke : new Stroke(hit, receiver.UseTexcoord2);

                        st.unRepeatedUv = hit.collider.GetType() == typeof(MeshCollider)
                            ? (receiver.UseTexcoord2 ? hit.textureCoord2 : hit.textureCoord).Floor()
                            : receiver.meshUvOffset;

                        if (_continious)
                            st.OnStrokeContiniousStart(hit, receiver.UseTexcoord2);

                        if (receiver.type == C_PaintingReceiver.RendererType.Skinned && receiver.skinnedMeshRenderer)
                            receiver.CreateCommandFor(st, _brush, subMesh).Paint();

                        else if (receiver.type == C_PaintingReceiver.RendererType.Regular && receiver.meshFilter)
                        {
                            if (_brush.GetBrushType(TexTarget.RenderTexture) == BrushTypes.Sphere.Inst)
                            {
                                receiver.CreateCommandFor(st, _brush, subMesh).Paint();
                            }
                            else
                                BrushTypes.Normal.Paint(rendTex, _brush, st);

                            break;
                        }

                    }
                    #endregion
                    #region TEXTURE SPACE BRUSH
                    else if (tex is Texture2D d)
                    {

                        if (hit.collider.GetType() != typeof(MeshCollider))
                            Debug.Log("Can't get UV coordinates from a Non-Mesh Collider");

                        BlitFunctions.Paint(receiver.UseTexcoord2 ? hit.textureCoord2 : hit.textureCoord, 1, d, Vector2.zero, Vector2.one, _brush);
                        var id = tex.GetTextureMeta();
                        _texturesNeedUpdate.AddIfNew(id);

                    }
                    #endregion
                    else 
                        Debug.Log(receiver.gameObject.name + " doesn't have any combination of paintable things setup on his PainterReciver.");
                }
        }

        private void LateUpdate()
        {
            foreach (var t in _texturesNeedUpdate)
                t.SetAndApply(); // True for Mipmaps. But best to disable mipmaps on textures or set this to false 

            _texturesNeedUpdate.Clear();
        }

        #region Inspector
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            var tf = transform;

            var pos = tf.position;

            var f = tf.forward;

            var ray = new Ray(pos, f);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                var painter = hit.transform.GetComponentInParent<PainterComponent>();

                Gizmos.color = !painter ? Color.red : Color.green;
                Gizmos.DrawLine(pos, hit.point);
            }
            else
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(pos, pos + f);
            }
        }
#endif

        private void Documentation()
        {
            ("I can paint on objects with PaintingReceiver script and:" +
            "Mesh Collider + any Texture" +
            "Skinned Mesh + any Collider + Render Texture" +
            "Also its better to use textures without mipMaps" +
            "Render Texture Painting may have artifacts if material has tiling or offset" +
            "Editing will be symmetrical if mesh is symmetrical" +
            "Brush type should be Sphere").PL().WriteBig();
        }

        void IPEGI.Inspect()
        {
            pegi.FullWindow.DocumentationClickOpen(Documentation);

            pegi.Nl();

            "Continious".PL().ToggleIcon(ref _continious).Nl();

            "Mode".PL(50).Edit_Enum(ref _mode).Nl();

            switch (_mode) 
            {
                case PaintingMode.ShootInMousePointedDirection:
                    if (!Camera.main)
                        "No Main Camera Found".PL().WriteWarning().Nl();
                    break;
            }

            if (!_continious)
            {
                "Bullets:".PL(50).Edit(ref _shoots, 1, 50).Nl();
                "Spread:".PL(50).Edit(ref _spread, 0f, 1f).Nl();
            }

            "Fire!".PL().Click().Nl().OnChanged(() => Paint(transform.position + transform.forward));

            _brush.Nested_Inspect();

            if (_brush.FallbackTarget == TexTarget.Texture2D)
            {
                "Script expects Render Texture terget".PL().WriteWarning().Nl();

                "Switch to Render Texture".PL().Click(()=> _brush.FallbackTarget = TexTarget.RenderTexture);
            }
            else if (_brush.GetBrushType(TexTarget.RenderTexture).GetType() != typeof(BrushTypes.Sphere))
            {
                "This component works best with Sphere Brush? also supports Normal Brush.".PL().Write_Hint();
            }

            if (!_brush.PaintingRGB)
                "Enable RGB, disable A to use faster Brush Shader (if painting to RenderTexture).".PL().Write_Hint();
        }
        #endregion
    }


    [PEGI_Inspector_Override(typeof(C_SimplePaintGunExample))] internal class PaintWithoutComponentEditor : PEGI_Inspector_Override { }

}