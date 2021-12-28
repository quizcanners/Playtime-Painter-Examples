using System.Collections.Generic;
using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;

namespace PainterTool.Examples
{

    public class RaycastOnCollisionPainter : MonoBehaviour, IPEGI
    {

        public Brush brush = new Brush();
        private readonly List<PaintingCollision> _paintingOn = new List<PaintingCollision>();

        private PaintingCollision GetPainterFrom(GameObject go)
        {

            foreach (var col in _paintingOn)
                if (col.painter.gameObject == go) return col;

            var pp = go.GetComponent<PainterComponent>();

            if (!pp) return null;

            var nCol = new PaintingCollision(pp);

            _paintingOn.Add(nCol);

            return nCol;
        }

        private void OnCollisionExit(Collision collision)
        {
            var p = GetPainterFrom(collision.gameObject);
            if (p == null) return;
            p.vector.MouseUpEvent = true;
            Paint(collision, p);
        }

        private void OnCollisionEnter(Collision collision)
        {

            var p = GetPainterFrom(collision.gameObject);
            if (p == null) return;
            p.vector.MouseDownEvent = true;
            Paint(collision, p);
        }

        private void OnCollisionStay(Collision collision)
        {
            var p = GetPainterFrom(collision.gameObject);
            if (p == null) return;
            Paint(collision, p);
        }

        private void Paint(Collision collision, PaintingCollision pCont)
        {
            if (pCont.painter.PaintCommand.SetBrush(brush).Is3DBrush)
            {
                var v = pCont.vector;
                v.posTo = transform.position;
                if (v.MouseDownEvent)
                    v.posFrom = v.posTo;

                var command = pCont.painter.PaintCommand;
                var originalStroke = command.Stroke;
                brush.Paint(command); 
                command.Stroke = originalStroke;
            }
            else
            {
                if (collision.contacts.Length <= 0) 
                    return;
                var cp = collision.contacts[0];
                var ray = new Ray(cp.point + cp.normal * 0.1f, -cp.normal);
                if (!collision.collider.Raycast(ray, out RaycastHit hit, 2f)) 
                    return;
                var v = pCont.vector;
                v.uvTo = hit.textureCoord;
                if (v.MouseDownEvent) v.uvFrom = v.uvTo;
                var command = pCont.painter.PaintCommand;
                var originalVector = command.Stroke;
                command.Stroke = pCont.vector;
                pCont.painter.SetTexTarget(brush);
                brush.Paint(command);
                command.Stroke = originalVector;
            }
        }

        #region Inspector

        public void Inspect()
        {

            pegi.FullWindow.DocumentationClickOpen(()=> "During collision will try to cast ray in the direction of that collision. " +
                                                       "If target has Playtime Painter Component this script will try to paint on it.", "How to use Raycast On Collision");

            if (Application.isPlaying)
                "Painting on {0} objects".F(_paintingOn.Count).PegiLabel().Nl();

            brush.Targets_PEGI(); pegi.Nl();
            brush.Mode_Type_PEGI(); pegi.Nl();
            brush.ColorSliders(); pegi.Nl();

        }

        #endregion
    }


    [PEGI_Inspector_Override(typeof(RaycastOnCollisionPainter))] internal class PainterCasterEditor : PEGI_Inspector_Override { }

}