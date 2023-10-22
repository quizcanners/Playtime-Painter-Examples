using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using System.Linq;
using UnityEngine;

namespace PainterTool.Examples
{
    [ExecuteInEditMode]
    public class C_PaintingReceiver : MonoBehaviour, IPEGI, INeedAttention
    {
        public enum RendererType { Regular, Skinned, Terrain }

        public RendererType type;

        public Mesh originalMesh;
        public MeshFilter meshFilter;
        public Renderer meshRenderer;
        public SkinnedMeshRenderer skinnedMeshRenderer;
        public Terrain terrain;
        public int materialIndex;
        public int preferedDamageTextureSize = 128;
        public bool autoSelectTextureSizeByVolume;
        public Texture originalTexture;
      
        public bool fromRtManager;
        public Vector2 meshUvOffset;
        [NonSerialized] private Material _originalMaterial;

        [SerializeField] private bool _useTexcoord2;
        [SerializeField] private Material _damagedMaterialVariant;
        [SerializeField] private string textureField = "";
        [SerializeField] private bool allowGettingSubmeshIndex;

        [NonSerialized] private Texture _texture;
        [NonSerialized] private ShaderProperty.TextureValue _textureProperty;

        public int GetTextureSizeByBoundingBox() 
        {
            if (!Renderer)
                return 128;

            var box = Renderer.bounds.extents.magnitude;

            var power = Mathf.RoundToInt(Mathf.Clamp(Mathf.Log(box * preferedDamageTextureSize, 2), 5, 10));

            return (int)Math.Pow(2, power);
        }

        public bool UseTexcoord2 
        {
            get 
            {
                if (_texture)
                    return _texture.GetTextureMeta()[TextureCfgFlags.Texcoord2];
                else
                    return _useTexcoord2;
            }

            set 
            {
                _useTexcoord2 = value;
                if (_texture)
                    _texture.GetTextureMeta()[TextureCfgFlags.Texcoord2] = _useTexcoord2;
            }
        }

        public Renderer Renderer => meshRenderer ? meshRenderer : skinnedMeshRenderer;

        private Material DamagedMaterial => _damagedMaterialVariant ? _damagedMaterialVariant : CurrentMaterial;

        public Material CurrentMaterial
        {
            get
            {
                switch (type) 
                {
                    case RendererType.Terrain:
                        return terrain.materialTemplate;
                    default:
                        var rend = Renderer;
                        return rend ? rend.sharedMaterials.TryGet(materialIndex) : null;
                }
            }

            private set
            {

                switch (type) 
                {
                    case RendererType.Terrain:

                        terrain.materialTemplate = value;

                        break;

                    default:
                        if (materialIndex >= Renderer.sharedMaterials.Length)
                            return;

                        var mats = Renderer.sharedMaterials;
                        mats[materialIndex] = value;
                        Renderer.materials = mats;
                        break;
            }
            }
        }

        public Material GetDamageMaterial()
        {
         
            if (!_originalMaterial)
            {
                _originalMaterial = CurrentMaterial;
                CurrentMaterial = Instantiate(_damagedMaterialVariant ? _damagedMaterialVariant : _originalMaterial);
            }

            return CurrentMaterial;
            
        }

        public bool UsesDamageMaterial => _originalMaterial && CurrentMaterial != _originalMaterial;

        public Texture GetTextureIfExist() => _texture;

        public Texture GetTexture()
        {
            if (_texture)
                return _texture;

            if (!GetDamageMaterial())
            {
                Debug.Log("No Material For Damage Found");
                return null;
            }

            var rtm = Singleton.Get<Singleton_TexturesPool>();

            if (!rtm)
            {
                QcLog.ChillLogger.LogErrorOnce("No {0} in the scene. Can't get tex".F(nameof(Singleton_TexturesPool)), key: "NoTexPool", gameObject); 
                return null;
            }


            var size = autoSelectTextureSizeByVolume ? GetTextureSizeByBoundingBox() : preferedDamageTextureSize;
            _texture = rtm.GetRenderTexture(size);
            _texture.GetTextureMeta()[TextureCfgFlags.Texcoord2] = _useTexcoord2;

            fromRtManager = true;

            var tex = originalTexture ? originalTexture : MatTex;

            if (tex)
                RenderTextureBuffersManager.Blit(tex, (RenderTexture)_texture);
            else
                Painter.Camera.Prepare(Color.black, (RenderTexture)_texture).Render();

            MatTex = _texture;

            return _texture;
        }

        public int GetSubmeshIndex(RaycastHit hit) 
        {
           

            if (allowGettingSubmeshIndex)
            {
                hit.TryGetSubMeshIndex_MAlloc(out int subMesh);
                return subMesh;
            }

            return 0;
            /*
            if (hit.collider.GetType() == typeof(MeshCollider))
            {
                subMesh = ((MeshCollider)hit.collider).sharedMesh.GetSubMeshNumber(hit.triangleIndex);
            }
            else
                subMesh = materialIndex;*/

         
        }

        public Painter.Command.Base CreateCommandFor(Stroke stroke, Brush brush, int subMesh = 0)
        {
            var tex = GetTexture();

            if (!tex) 
            {
                Debug.LogError("No Texture for " + gameObject.name, gameObject);
            }

            if (skinnedMeshRenderer) 
            {
                return new Painter.Command.WorldSpace(stroke, GetTexture(), brush, skinnedMeshRenderer, subMesh);
            } 

            if (type == RendererType.Terrain) 
            {
                return new Painter.Command.UV(stroke, GetTexture(), brush);
            }
            
            return new Painter.Command.WorldSpace(stroke, GetTexture().GetTextureMeta(), brush,
                mesh: originalMesh ? originalMesh : Mesh, subMesh, gameObject);
        }

        public Stroke CreateStroke(ContactPoint point, Vector3 strokeVector = default)
        {
            return new Stroke(point, strokeVector: strokeVector);
        }

        public Stroke CreateStroke(RaycastHit hit, Vector3 strokeVector = default) 
        {
            var st = new Stroke(hit, UseTexcoord2);

            st.posTo += strokeVector;

            st.unRepeatedUv = hit.collider.GetType() == typeof(MeshCollider)
                ? (UseTexcoord2 ? hit.textureCoord2 : hit.textureCoord).Floor()
                : meshUvOffset;

            return st;
        }

        public void ResetEffect()
        {
            if (_originalMaterial) 
            {
                CurrentMaterial = _originalMaterial;
                _originalMaterial = null;
            }

            if (fromRtManager)
            {
                fromRtManager = false;
               
                if (_texture && !Singleton.Try<Singleton_TexturesPool>(
                    onFound: srv => srv.ReturnOne((RenderTexture)_texture),
                    logOnServiceMissing: false))
                {
                    Destroy(_texture);
                }

                _texture = null;
                return;
            }

            if (!_texture)
                return;

            if (!originalTexture)
            {
                Debug.Log("Original Texture is not defined");
                return;
            }

            if (originalTexture.GetType() != typeof(Texture2D))
            {
                Debug.Log("There was no original Texture assigned to edit.");
            }

            var t2D = _texture as Texture2D;

            if (t2D)
            {
                t2D.SetPixels32(((Texture2D)originalTexture).GetPixels32());
                t2D.Apply(true);
            }
            else
                RenderTextureBuffersManager.Blit(originalTexture, (RenderTexture)_texture);

        }

        private string TexturePropertyName
        {
            set
            {
                textureField = value;
                _textureProperty = new ShaderProperty.TextureValue(value);
            }
        }

        private ShaderProperty.TextureValue TextureId
        {
            get
            {

                _textureProperty ??= new ShaderProperty.TextureValue(textureField);

                return _textureProperty;
            }
        }

        private Mesh Mesh => skinnedMeshRenderer ? skinnedMeshRenderer.sharedMesh : (meshFilter ? meshFilter.sharedMesh : null);

        private Texture MatTex
        {
            get
            {
                if (!CurrentMaterial) return null;
                return CurrentMaterial.Has(TextureId) ? CurrentMaterial.Get(TextureId) : CurrentMaterial.mainTexture;
            }
            set
            {
                if (CurrentMaterial.Has(TextureId))
                    CurrentMaterial.Set(TextureId, value);
                else
                {
                    CurrentMaterial.mainTexture = value;
                    QcLog.ChillLogger.LogErrorOnce(()=> "No {0} target ID on the material, trying to set main texture.".F(TextureId.ToString()), key: "notid");
                }
            }
        }

        void OnEnable()
        {
            if (!Application.isPlaying)
                Refresh();

            if (Application.isPlaying && originalTexture && _texture && (_texture.GetType() == typeof(RenderTexture)))
                RenderTextureBuffersManager.Blit(originalTexture, (RenderTexture)_texture);

            if (DamagedMaterial && !DamagedMaterial.Has(TextureId))
                _textureProperty = null;
        }

        private void Refresh()
        {
            if (!meshFilter)
                meshFilter = GetComponent<MeshFilter>();

            if (!meshRenderer)
                meshRenderer = GetComponent<Renderer>();

            if (!skinnedMeshRenderer)
                skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        }

        private void OnDisable()
        {
            if (fromRtManager) 
                ResetEffect();

        }

        #region Inspector
        [SerializeField] private bool _showOptional;

        public virtual void Inspect()
        {

            if (autoSelectTextureSizeByVolume)
                "Box size: {0}x{1}px = {2}".F(Renderer.bounds.extents.magnitude, preferedDamageTextureSize, GetTextureSizeByBoundingBox()).PegiLabel().Nl();

         //   "Log: {0}".F(Mathf.log(logTest, 2)).PegiLabel().Edit(ref logTest);

            if (_texture && (!MatTex || MatTex != _texture))
            {
                "Target texture not set ont he Material".PegiLabel().WriteWarning();
                "Clear target texture".PegiLabel().Click(() =>
                {
                    _texture = null;
                    MatTex = null;
                });
            }


            pegi.Nl();

            "Damaged Material Variant".PegiLabel().Edit(ref _damagedMaterialVariant).Nl();

            pegi.Nl();

            if (!Painter.Camera)
            {
                "No Painter Camera found".PegiLabel().WriteWarning();

                return;
            }
            
            pegi.FullWindow.DocumentationClickOpen(()=> "Works with PaintWithoutComponent script. This lets you configure how painting will be received." +
                                                       " PaintWithoutComponent.cs is usually attached to a main camera (if painting in first person). Current Texture: " + TextureId, "About Painting Receiver");

            if (Icon.Refresh.Click("Find Components automatically"))
                Refresh();

            if ("Renderer GetBrushType:".PegiLabel(90).Edit_Enum(ref type).Nl())
            {
                switch (type)
                {

                    case RendererType.Skinned:
                        if (!skinnedMeshRenderer)
                            skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
                        break;

                    case RendererType.Regular: 
                        if (!meshFilter)
                        {
                            meshFilter = GetComponent<MeshFilter>();
                            meshRenderer = GetComponent<MeshRenderer>();
                        }
                        break;

                    case RendererType.Terrain:
                        if (!terrain)
                            terrain = GetComponent<Terrain>();
                        break;
            }
            }

            switch (type)
            {
                case RendererType.Skinned:
                    "   Skinned Mesh Renderer".PegiLabel(90).Edit(ref skinnedMeshRenderer).Nl();
                    break;

                case RendererType.Regular:
                    "   Mesh Filter".PegiLabel(90).Edit( ref meshFilter).Nl();
                    "   Renderer".PegiLabel(90).Edit( ref meshRenderer).Nl();

                    if (meshFilter && meshFilter.sharedMesh) 
                    {
                        var mc = GetComponent<MeshCollider>();
                        if (mc && meshFilter.sharedMesh != mc.sharedMesh) 
                        {
                            "Mesh Collider has a different mesh".PegiLabel().WriteWarning().Nl();
                            if ("Copy Mesh".PegiLabel().Click())
                                mc.sharedMesh = meshFilter.sharedMesh;

                            pegi.Nl();
                        }
                    }
                    break;
                case RendererType.Terrain:
                    "Terrain".PegiLabel().Edit(ref terrain).Nl();

                    break;
            }

            var r = Renderer;

            if ((r && r.sharedMaterials.Length > 1) || materialIndex != 0)
                "   Material".PegiLabel(width: 80).Select_Index(ref materialIndex, r.sharedMaterials).Nl();

            if (DamagedMaterial)
            {
                var lst = DamagedMaterial.MyGetTextureProperties_Editor();

                if ("   Target Texture".PegiLabel(width: 80).Select(ref _textureProperty, lst).Nl())
                    TexturePropertyName = _textureProperty.ToString();
            }

            if (type != RendererType.Terrain && gameObject.isStatic && !originalMesh)
            {
                "For STATIC Game Objects original mesh is needed:".PegiLabel().WriteWarning();

                pegi.Nl();

                if (meshFilter && Icon.Search.Click("Find mesh"))
                    originalMesh = meshFilter.sharedMesh;
            }

            if (gameObject.isStatic)
                "  Original Mesh".PegiLabel("Static objects use Combined mesh, so original one will be needed for painting", 50).Edit(ref originalMesh).Nl();

            var uv2 = UseTexcoord2;
            if ("  Use second texture coordinates".PegiLabel("If shader uses texcoord2 (Baked Light) to display damage, turn this ON.").ToggleIcon( ref uv2).Nl())
                UseTexcoord2 = uv2;

            if (UseTexcoord2 && Mesh) 
            {
                if (!Mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord1))
                    "{0} doesn't have UV2".F(Mesh.name).PegiLabel().WriteWarning().Nl();
            }

            if (CurrentMaterial)
            {
                if (!DamagedMaterial.Has(TextureId)) // && !Material.mainTexture)
                    "No Material Property Selected".PegiLabel().WriteWarning().Nl();
                else
                {
                    if (_texture)
                    {
                        var t2D = _texture as Texture2D;

                        if (t2D)
                        {
                            Icon.Done.Draw();
                            "CPU brush will work if object has MeshCollider".PegiLabel().Nl();

                            if (originalTexture)
                            {
                                var ot2D = originalTexture as Texture2D;

                                if (ot2D)
                                {
                                    if ((ot2D.width == t2D.width) && (ot2D.height == t2D.height))
                                    {
                                        if (("Undo Changes".PegiLabel().Click()).Nl())
                                            ResetEffect();
                                    }
                                    else "Original and edited texture are not of the same size".PegiLabel().Nl();
                                }
                                else "Original Texture is not a Texture 2D".PegiLabel().Nl();
                            }
                        }
                        else
                        {
                            if (Renderer)
                            {
                                Icon.Done.Draw();
                                "Will paint if object has any collider".PegiLabel().Nl();
                                if (skinnedMeshRenderer)
                                {
                                    "Colliders should be placed close to actual mesh".PegiLabel().Nl();
                                    "Otherwise brush size may be too small to reach the mesh".PegiLabel().Nl();
                                }
                            }
                            else
                                "Render Texture Painting needs Skinned Mesh or Mesh Filter to work".PegiLabel().Nl();

                            if ((originalTexture) && ("Undo Changes".PegiLabel().Click().Nl()))
                                ResetEffect();
                        }
                    }
                    else
                    {
                        var rtm = Singleton.Get<Singleton_TexturesPool>();

                        "Auto Size ({0})".F(GetTextureSizeByBoundingBox()).PegiLabel().ToggleIcon(ref autoSelectTextureSizeByVolume).Nl();

                         (autoSelectTextureSizeByVolume ? "Prefered pixels per meter" : "Damage Texture Size").PegiLabel().SelectPow2(ref preferedDamageTextureSize, 16, 2048).Nl();
                        
                        if (rtm)
                        {
                            "Render Texture Pool will be used to get texture".PegiLabel().Nl();
                            if (!Renderer) "! Renderer needs to be Assigned.".PegiLabel().Nl();
                            else
                            {
                                Icon.Done.Draw();
                                "COMPONENT SET UP CORRECTLY".PegiLabel().Write();
                                if (fromRtManager && "Restore".PegiLabel().Click())
                                    ResetEffect();
                                pegi.Nl();
                            }
                        }
                        else
                        {
                            "No Render Texture Pool found. GPU painting needs a Render Texture".PegiLabel().WriteWarning();
                            "Create".PegiLabel().Click().Nl().OnChanged(() =>
                                pegi.GameView.ShowNotification((Singleton_TexturesPool.ForcedInstance.gameObject.name + " created")));
                        }
                    }
                }
            }
            else "No material found".PegiLabel().Nl();


            var col = GetComponent<Collider>();

            if (!col) 
            {
                col = GetComponentInChildren<Collider>();
                if (!col)
                    "Collider not found".PegiLabel().WriteWarning();
            }

            if (col) 
            {
                if (col.GetType() != typeof(MeshCollider))
                    "The colider is {0}. Will only work with sphere brush.".F(col.GetType()).PegiLabel().Write_Hint();
                else if (col.enabled == false)
                    "The collider is disabled".PegiLabel().Write_Hint();
            }

            pegi.Nl();

            if (Application.isPlaying)
                "Target Texture".PegiLabel("If not using Render Textures Pool", 120).Edit(ref _texture);

               // if (Renderer && Material && "Find".PegiLabel().Click())
                 //   texture = MatTex;
            

            if (_texture)
                Icon.Delete.Click(ResetEffect);
            
            pegi.Nl();

            if ("Advanced".PegiLabel().IsFoldout(ref _showOptional).Nl())
            {
                if (_texture || !MatTex)
                    "Start Texture:".PegiLabel("Copy of this texture will be modified.", 110).Edit(ref originalTexture).Nl();
                
                if (!_texture || _texture.GetType() == typeof(RenderTexture))
                {
                    "Mesh UV Offset".PegiLabel(toolTip: "Some Meshes have UV coordinates with displacement for some reason. " +
                        "If your object doesn't use a mesh collider to provide a UV offset, this will be used.",  width: 80).Edit(ref meshUvOffset).Nl();
                    if (Mesh && "Offset from Mesh".PegiLabel().Click().Nl())
                    {
                        var firstVertInSubmeshIndex = Mesh.GetTriangles(materialIndex)[0];
                        meshUvOffset = UseTexcoord2 ? Mesh.uv2[firstVertInSubmeshIndex] : Mesh.uv[firstVertInSubmeshIndex];

                        meshUvOffset = new Vector2((int)meshUvOffset.x, (int)meshUvOffset.y);

                        pegi.GameView.ShowNotification("Mesh Offset is " + meshUvOffset);
                    }
                }
            }
        }

        public string NeedAttention()
        {
            if (type != RendererType.Terrain && gameObject.isStatic && !originalMesh)
            {
                return "For STATIC Game Objects original mesh is needed.";
            }


            return null;
        }

        #endregion

        public class ReferenceForContinious 
        {
            public Stroke Stroke;
            public Painter.Command.Base Command;
            public Gate.UnityTimeUnScaled DeltaTime = new(Gate.InitialValue.StartArmed);

            public void OnContactDetected() => DeltaTime.Update();

            public bool TryPaintIfPositionChanged(Vector3 newPos, float minDelta, float timeLessThen = 1) 
            {
                if (DeltaTime.TryUpdateIfTimePassed(timeLessThen))
                {
                    Stroke.OnStrokeEnd();
                    return false;
                }

                if (!Stroke.firstStroke && Vector3.Distance(newPos, Stroke.posFrom) < minDelta)
                {
                    return false;
                }

                Stroke.OnStrokeStart(newPos);
                Command.Paint();

                return true;
            }

            public ReferenceForContinious(C_PaintingReceiver reciever, Brush brush, Vector3 point) 
            {
                Stroke = new Stroke(point);
                Command = reciever.CreateCommandFor(Stroke, brush);
            }
        }

        public ReferenceForContinious GetReferenceForContinious(Brush brush, Vector3 point) 
        {
            return new ReferenceForContinious(this, brush, point);
        }

    }

    public static class PaintingReceiverExtensions
    {
        public static C_PaintingReceiver GetByHit(this C_PaintingReceiver[] receivers, RaycastHit hit, out int subMesh) 
        {
            if (receivers.IsNullOrEmpty()) 
            {
                subMesh = 0;
                return null;
            }

            

            var first = receivers[0];

            /*
            if (hit.TryGetSubMeshIndex_MAlloc(out subMesh))
            {
                if (receivers.Length > 1)
                {
                    var mats = first.Renderer.materials;
                    var material = mats[subMesh % mats.Length];
                    return receivers.FirstOrDefault(r => r.CurrentMaterial == material);
                }
            }
            else*/
                subMesh = first.materialIndex;


            return first;
        }
    }

    [PEGI_Inspector_Override(typeof(C_PaintingReceiver))] internal class PaintingReceiverEditor : PEGI_Inspector_Override { }
}