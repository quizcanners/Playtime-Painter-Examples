using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using UnityEngine;

namespace PlaytimePainter.Examples
{

    [ExecuteInEditMode]
    public class PaintingReceiver : MonoBehaviour, IPEGI
    {

        public enum RendererType { Regular, Skinned }

        public RendererType type;

        public Mesh originalMesh;
        public MeshFilter meshFilter;
        public Renderer meshRenderer;
        public SkinnedMeshRenderer skinnedMeshRenderer;
        public int materialIndex;

        public RenderTexture TryGetRenderTexture()
        {
           return texture.GetType() == typeof(RenderTexture) ? (RenderTexture)texture : null;
        }

        public PaintCommand.WorldSpaceBase TryMakePaintCommand(Stroke stroke, Brush brush, int subMesh) 
            => new PaintCommand.WorldSpace(stroke, TryGetRenderTexture().GetTextureMeta(), brush,
                originalMesh
                    ? originalMesh
                    : meshFilter.sharedMesh,
                subMesh,
                gameObject
            );
        

        [NonSerialized]private ShaderProperty.TextureValue _textureProperty;

        [SerializeField] private string textureField = "";
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

                if (_textureProperty == null)
                    _textureProperty = new ShaderProperty.TextureValue(textureField);

                return _textureProperty;
            }
        }


        private Mesh Mesh => skinnedMeshRenderer ? skinnedMeshRenderer.sharedMesh : (meshFilter ? meshFilter.sharedMesh : null);
        public Renderer Renderer => meshRenderer ? meshRenderer : skinnedMeshRenderer;

        public Material Material
        {
            get
            {
                var rend = Renderer;

                return rend ? rend.sharedMaterials.TryGet(materialIndex) : null;
            }


            private set
            {
                if (materialIndex >= Renderer.sharedMaterials.Length) return;

                var mats = Renderer.sharedMaterials;
                mats[materialIndex] = value;
                Renderer.materials = mats;
            }
        }

        private Texture MatTex
        {
            get
            {
                if (!Material) return null;
                return Material.Has(TextureId) ? Material.Get(TextureId) : Material.mainTexture;

            }
            set
            {
                if (Material.Has(TextureId))
                    Material.Set(TextureId, value);
                else
                {
                    Material.mainTexture = value;
                    QcUnity.ChillLogger.LogErrorOnce(()=> "No {0} target ID on the material, trying to set main texture.".F(pegi.GetNameForInspector(TextureId)), key: "notid");
                }
            }
        }

        [NonSerialized] public Texture texture;
        public Texture originalTexture;
        public bool useTexcoord2;
        public bool fromRtManager;
        public Vector2 meshUvOffset;
        public Material originalMaterial;

        private void OnEnable()
        {
            if (!Application.isPlaying)
                Refresh();

            if (Application.isPlaying && originalTexture && texture && (texture.GetType() == typeof(RenderTexture)))
                PlaytimePainter_RenderTextureBuffersManager.Blit(originalTexture, (RenderTexture)texture);

            if (Material && !Material.Has(TextureId))
                _textureProperty = null;

        }

        public Texture GetTexture()
        {

            if (texture)
                return texture;

            var rtm = Singleton.Get<PlaytimePainter_TexturesPool>();

            if (!Material)
            {
                Debug.Log("No Material ");
                return null;
            }

            if (!rtm) return texture;

            originalMaterial = Material;

            texture = rtm.GetRenderTexture();

            fromRtManager = true;

            Material = Instantiate(originalMaterial);

            var tex = originalTexture ? originalTexture : MatTex;
            if (tex)
                PlaytimePainter_RenderTextureBuffersManager.Blit(tex, (RenderTexture)texture);
            else
                PainterCamera.GetOrCreate.Render(Color.black, (RenderTexture)texture);

            MatTex = texture;

            texture.GetTextureMeta().UseTexCoord2 = useTexcoord2;

            return texture;
        }

        public void Restore()
        {

            if (fromRtManager && (originalMaterial))
            {
                fromRtManager = false;
                Material = originalMaterial;
                originalMaterial = null;
                if (Singleton.Try<PlaytimePainter_TexturesPool>(srv => srv.ReturnOne((RenderTexture)texture)))
                {
                    Destroy(texture);
                    Debug.LogWarning("No Texture pool to return texture");
                }
                texture = null;
                return;
            }

            if (!texture)
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

            var t2D = texture as Texture2D;

            if (t2D)
            {
                t2D.SetPixels32(((Texture2D)originalTexture).GetPixels32());
                t2D.Apply(true);
            }
            else
                PlaytimePainter_RenderTextureBuffersManager.Blit(originalTexture, (RenderTexture)texture);

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
                Restore();

        }

        #region Inspector
        [SerializeField] private bool _showOptional;

        public virtual void Inspect()
        {
            if (texture && (!MatTex || MatTex != texture))
            {
                "Target texture not set ont he Material".PegiLabel().writeWarning();
                "Clear target texture".PegiLabel().Click(() =>
                {
                    texture = null;
                    MatTex = null;
                });
            }

            pegi.nl();

            if (!PainterCamera.GetOrCreate)
            {
                "No Painter Camera found".PegiLabel().writeWarning();

                if ("Refresh".PegiLabel().Click())
                    PainterClass.applicationIsQuitting = false;

                return;
            }
            
            pegi.FullWindow.DocumentationClickOpen(()=> "Works with PaintWithoutComponent script. This lets you configure how painting will be received." +
                                                       " PaintWithoutComponent.cs is usually attached to a main camera (if painting in first person). Current Texture: " + TextureId, "About Painting Receiver");

            if (icon.Refresh.Click("Find Components automatically"))
                Refresh();

            if ("Renderer GetBrushType:".PegiLabel(90).editEnum(ref type).nl())
            {
                if (type == RendererType.Skinned && !skinnedMeshRenderer)
                    skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();

                if (type == RendererType.Regular && !meshFilter)
                {
                    meshFilter = GetComponent<MeshFilter>();
                    meshRenderer = GetComponent<MeshRenderer>();
                }
            }

            switch (type)
            {
                case RendererType.Skinned:
                    "   Skinned Mesh Renderer".PegiLabel(90).edit(ref skinnedMeshRenderer).nl();
                    break;

                case RendererType.Regular:
                    "   Mesh Filter".PegiLabel(90).edit( ref meshFilter).nl();
                    "   Renderer".PegiLabel(90).edit( ref meshRenderer).nl();
                    break;
            }

            var r = Renderer;

            if ((r && r.sharedMaterials.Length > 1) || materialIndex != 0)
                "   Material".PegiLabel(width: 80).select_Index(ref materialIndex, r.sharedMaterials).nl();

            if (Material)
            {
                var lst = Material.MyGetTextureProperties_Editor();

                if ("   Target Texture".PegiLabel(width: 80).select(ref _textureProperty, lst).nl())
                    TexturePropertyName = _textureProperty.GetReadOnlyName();
            }

            if (gameObject.isStatic && !originalMesh)
            {
                "For STATIC Game Objects original mesh is needed:".PegiLabel().writeHint();

                pegi.nl();

                if (meshFilter && icon.Search.Click("Find mesh"))
                    originalMesh = meshFilter.sharedMesh;
            }

            if (gameObject.isStatic)
                "  Original Mesh".PegiLabel("Static objects use Combined mesh, so original one will be needed for painting", 50).edit(ref originalMesh).nl();

            if ("  Use second texture coordinates".PegiLabel("If shader uses texcoord2 (Baked Light) to display damage, turn this ON.").toggleIcon( ref useTexcoord2).nl() && texture)
                texture.GetTextureMeta().UseTexCoord2 = useTexcoord2;

            if (Material)
            {
                if (!Material.Has(TextureId) && !Material.mainTexture)
                    "No Material Property Selected and no MainTex on Material".PegiLabel().nl();
                else
                {
                    if (texture)
                    {
                        var t2D = texture as Texture2D;

                        if (t2D)
                        {
                            icon.Done.draw();
                            "CPU brush will work if object has MeshCollider".PegiLabel().nl();

                            if (originalTexture)
                            {
                                var ot2D = originalTexture as Texture2D;

                                if (ot2D)
                                {
                                    if ((ot2D.width == t2D.width) && (ot2D.height == t2D.height))
                                    {
                                        if (("Undo Changes".PegiLabel().Click()).nl())
                                            Restore();
                                    }
                                    else "Original and edited texture are not of the same size".PegiLabel().nl();
                                }
                                else "Original Texture is not a Texture 2D".PegiLabel().nl();
                            }
                        }
                        else
                        {
                            if (Renderer)
                            {
                                icon.Done.draw();
                                "Will paint if object has any collider".PegiLabel().nl();
                                if (skinnedMeshRenderer)
                                {
                                    "Colliders should be placed close to actual mesh".PegiLabel().nl();
                                    "Otherwise brush size may be too small to reach the mesh".PegiLabel().nl();
                                }
                            }
                            else
                                "Render Texture Painting needs Skinned Mesh or Mesh Filter to work".PegiLabel().nl();

                            if ((originalTexture) && ("Undo Changes".PegiLabel().Click().nl()))
                                Restore();
                        }
                    }
                    else
                    {
                        var rtm = Singleton.Get<PlaytimePainter_TexturesPool>();

                        if (rtm)
                        {
                            "Render Texture Pool will be used to get texture".PegiLabel().nl();
                            if (!Renderer) "! Renderer needs to be Assigned.".PegiLabel().nl();
                            else
                            {
                                icon.Done.draw();
                                "COMPONENT SET UP CORRECTLY".PegiLabel().write();
                                if (fromRtManager && "Restore".PegiLabel().Click())
                                    Restore();
                                pegi.nl();
                            }
                        }
                        else
                        {
                            "No Render Texture Pool found".PegiLabel().write();
                            "Create".PegiLabel().Click().nl().OnChanged(() =>
                                pegi.GameView.ShowNotification((PlaytimePainter_TexturesPool.ForceInstance.gameObject.name + " created")));
                        }
                    }
                }
            }
            else "No material found".PegiLabel().nl();

            /*if ("On Disable".PegiLabel().Click().nl())
          {
              OnDisable();
          }*/

            "Target Texture".PegiLabel("If not using Render Textures Pool", 120).edit( ref texture);
            if (Renderer && Material && "Find".PegiLabel().Click())
                texture = MatTex;

            if (texture)
                icon.Delete.Click(Restore);
            
            pegi.nl();

            if ("Advanced".PegiLabel().isFoldout(ref _showOptional).nl())
            {
                if (texture || !MatTex)
                    "Start Texture:".PegiLabel("Copy of this texture will be modified.", 110).edit(ref originalTexture).nl();
                
                if (!texture || texture.GetType() == typeof(RenderTexture))
                {
                    "Mesh UV Offset".PegiLabel(toolTip: "Some Meshes have UV coordinates with displacement for some reason. " +
                        "If your object doesn't use a mesh collider to provide a UV offset, this will be used.",  width: 80).edit(ref meshUvOffset).nl();
                    if (Mesh && "Offset from Mesh".PegiLabel().Click().nl())
                    {
                        var firstVertInSubmeshIndex = Mesh.GetTriangles(materialIndex)[0];
                        meshUvOffset = useTexcoord2 ? Mesh.uv2[firstVertInSubmeshIndex] : Mesh.uv[firstVertInSubmeshIndex];

                        meshUvOffset = new Vector2((int)meshUvOffset.x, (int)meshUvOffset.y);

                        pegi.GameView.ShowNotification("Mesh Offset is " + meshUvOffset);
                    }
                }
            }
        }

        #endregion
    }

    [PEGI_Inspector_Override(typeof(PaintingReceiver))] internal class PaintingReceiverEditor : PEGI_Inspector_Override { }
}