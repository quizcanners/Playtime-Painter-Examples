using System.Collections.Generic;
using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;

namespace PainterTool.Examples { 

    [ExecuteInEditMode]
    public class PainterBall : MonoBehaviour  , IPEGI {
        
        public MeshRenderer rendy;
        public Rigidbody rigid;
        public SphereCollider _collider;

		public List<PaintingCollision> paintingOn = new List<PaintingCollision>();
        public Brush brush = new Brush();

        private void TryGetPainterFrom(GameObject go) {

            var target = go.GetComponent<PainterComponent>();

            if (!target || target.TextureEditingBlocked) 
                return;

            var col = new PaintingCollision(target);
            paintingOn.Add(col);
            col.vector.posFrom = transform.position;
            col.vector.firstStroke = true;
            target.UpdateOrSetTexTarget(TexTarget.RenderTexture);
        }

        public void OnCollisionEnter(Collision collision) => TryGetPainterFrom(collision.gameObject);
        
        public void OnTriggerEnter(Collider enteredCollider) => TryGetPainterFrom(enteredCollider.gameObject);
         
        private void TryRemove(GameObject go)
        {
            foreach (var p in paintingOn)
                if (p.painter.gameObject == go)
                {
                    paintingOn.Remove(p);
                    return;
                }
        }

        public void OnTriggerExit(Collider exitedCollider) => TryRemove(exitedCollider.gameObject);
        
        public void OnCollisionExit(Collision exitedCollider) => TryRemove(exitedCollider.gameObject);
        
        public void OnEnable()  
        {
            brush.SetBrushType(TexTarget.RenderTexture, BrushTypes.Sphere.Inst);

            if (!rendy) 
                rendy = GetComponent<MeshRenderer>();

            if (!rigid)
                rigid = GetComponent<Rigidbody>();

            if (!_collider)
                _collider = GetComponent<SphereCollider>();

            if (rendy)
                rendy.sharedMaterial.color = brush.Color;

            brush.FallbackTarget = TexTarget.RenderTexture;
        }

        private void Update() {

            brush.brush3DRadius = transform.lossyScale.x*1.4f;

			foreach (var col in paintingOn)
            {
				var p = col.painter;

                if (!p.PaintCommand.SetBrush(brush).Is3DBrush) 
                    continue;

                var v = col.vector;
                v.posTo = transform.position;

                brush.Paint(p.PaintCommand.SetStroke(v));//v, p);
            }
        }

        public void Inspect()
        {

            pegi.FullWindow.DocumentationClickOpen(()=> "When colliding with other object will try to use sphere brush to paint on them." +
                                                       "Targets need to have PlaytimePainter component", "About Painter Ball");
     
            if (Application.isPlaying)
                "Painting on {0} objects".F(paintingOn.Count).PegiLabel().Nl();

            if (_collider.isTrigger && "Set as Rigid Collider object".PegiLabel().Click().Nl())
            {
                _collider.isTrigger = false;
                rigid.isKinematic = false;
                rigid.useGravity = true;
            }

            if (!_collider.isTrigger)
                "Set as Trigger".PegiLabel().Click().Nl().OnChanged(() =>
                    {
                        _collider.isTrigger = true;
                        rigid.isKinematic = true;
                        rigid.useGravity = false;
                    });

            var size = transform.localScale.x;

            if ("Size:".PegiLabel("Size of the ball", 50).Edit( ref size, 0.1f, 10).Nl())
                transform.localScale = Vector3.one * size;

            const string ballHint = "PaintBall_brushHint";

            "Painter ball made for World Space Brushes only".PegiLabel().WriteOneTimeHint(ballHint);

            if ((pegi.Nested_Inspect(brush.Targets_PEGI).Nl()) | (pegi.Nested_Inspect(brush.Mode_Type_PEGI).Nl()))
            {
                if (brush.FallbackTarget == TexTarget.Texture2D || !brush.Is3DBrush())
                {
                    brush.FallbackTarget = TexTarget.RenderTexture;
                    brush.SetBrushType(TexTarget.RenderTexture, BrushTypes.Sphere.Inst);

                    pegi.ResetOneTimeHint(ballHint);
                }
            }

            if (pegi.Nested_Inspect(brush.ColorSliders))
                rendy.sharedMaterial.color = brush.Color;
        }

    }

    public class PaintingCollision
    {
        public Stroke vector;
        public PainterComponent painter;

        public PaintingCollision(PainterComponent p)
        {
            painter = p;
            vector = new Stroke();
        }
    }


    [PEGI_Inspector_Override(typeof(PainterBall))] internal class PainterBallEditor : PEGI_Inspector_Override { }

}
