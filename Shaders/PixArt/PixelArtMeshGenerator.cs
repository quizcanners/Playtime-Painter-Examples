﻿using System.Collections.Generic;
using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;

namespace PainterTool.Examples {

    [ExecuteInEditMode]
    public class PixelArtMeshGenerator : MonoBehaviour, IPEGI {
        private static int width = 8;
        private static float halfPix;
        private static Vert[] verts;
        private static readonly List<int> tris = new List<int>();

      //  public Vector4 uvSector = new Vector4(0, 0, 1, 1);
        public int testWidth = 8;
        public float thickness;
        
        public MeshFilter meshFilter;
        public MeshCollider meshCollider;

        private enum PicV { lup = 0, rup = 1, rdwn = 2, ldwn = 3 }

        private class Vert
        {
            public Vector3 pos;
            public Vector4 uv;

            public Vert(int x, int y, PicV p, float borderPercent)
            {
                uv = new Vector4(halfPix + x / (float)width, halfPix + y / (float)width                                                     // normal coordinate

                    , halfPix + x / (float)width, halfPix + y / (float)width); // with center coordinate

                pos = new Vector3(uv.x - 0.5f, uv.y - 0.5f, 0);

                float off = halfPix * (1 - borderPercent);

                Vector3 offf = Vector3.zero;

                switch (p)
                {
                    case PicV.ldwn: offf += new Vector3(-off, off, 0); break;
                    case PicV.lup: offf += new Vector3(-off, -off, 0); break;
                    case PicV.rdwn: offf += new Vector3(off, off, 0); break;
                    case PicV.rup: offf += new Vector3(off, -off, 0); break;
                }

                pos += offf;

                uv.x += offf.x;
                uv.y += offf.y;
                


            }
        }

        private static int GetIndOf(int x, int y, PicV p) => (y * width + x) * 4 + (int)p;


        private void JoinDiagonal(int x, int y)
        {
            tris.Add(GetIndOf(x, y, PicV.rdwn));
            tris.Add(GetIndOf(x + 1, y, PicV.ldwn));
            tris.Add(GetIndOf(x, y + 1, PicV.rup));

            tris.Add(GetIndOf(x, y + 1, PicV.rup));
            tris.Add(GetIndOf(x + 1, y, PicV.ldwn));
            tris.Add(GetIndOf(x + 1, y + 1, PicV.lup));
        }

        private void JoinDown(int x, int y)
        {
            tris.Add(GetIndOf(x, y, PicV.ldwn));
            tris.Add(GetIndOf(x, y, PicV.rdwn));
            tris.Add(GetIndOf(x, y + 1, PicV.lup));

            tris.Add(GetIndOf(x, y, PicV.rdwn));
            tris.Add(GetIndOf(x, y + 1, PicV.rup));
            tris.Add(GetIndOf(x, y + 1, PicV.lup));

        }

        private void JoinRight(int x, int y)
        {
            tris.Add(GetIndOf(x, y, PicV.rup));
            tris.Add(GetIndOf(x + 1, y, PicV.lup));
            tris.Add(GetIndOf(x, y, PicV.rdwn));

            tris.Add(GetIndOf(x + 1, y, PicV.lup));
            tris.Add(GetIndOf(x + 1, y, PicV.ldwn));
            tris.Add(GetIndOf(x, y, PicV.rdwn));
        }

        private void FillPixel(int x, int y)
        {
            tris.Add(GetIndOf(x, y, PicV.lup));
            tris.Add(GetIndOf(x, y, PicV.rup));
            tris.Add(GetIndOf(x, y, PicV.ldwn));

            tris.Add(GetIndOf(x, y, PicV.rup));
            tris.Add(GetIndOf(x, y, PicV.rdwn));
            tris.Add(GetIndOf(x, y, PicV.ldwn));
        }

        public Mesh GenerateMesh(int w)
        {
            width = w;
            halfPix = 0.5f / width;

            Mesh m = new Mesh();

            int pixls = width * width;

            verts = new Vert[pixls * 4];
            tris.Clear();

            Vector3[] fverts = new Vector3[verts.Length];
            List<Vector4> uvs = new List<Vector4>();//[verts.Length];

            for (int i = 0; i < verts.Length; i++)
                uvs.Add(new Vector4());

            for (int x = 0; x < width; x++)
                for (int y = 0; y < width; y++)
                {
                    for (int p = 0; p < 4; p++)
                    {
                        int ind = GetIndOf(x, y, (PicV)p);
                        verts[ind] = new Vert(x, y, (PicV)p, thickness);
                        fverts[ind] = verts[ind].pos;
                        uvs[ind] = verts[ind].uv;
                    }

                    FillPixel(x, y);
                    if (thickness > 0)
                    {
                        thickness = Mathf.Min(thickness, 0.9f);
                        if (x < width - 1) JoinRight(x, y);
                        if (y < width - 1) JoinDown(x, y);
                        if ((x < width - 1) && (y < width - 1))
                            JoinDiagonal(x, y);
                    }
                }

            m.vertices = fverts;
            m.SetUVs(0, uvs);
            m.triangles = tris.ToArray();

            return m;
        }

        private void OnEnable() {
            if (!meshFilter)
                meshFilter = GetComponent<MeshFilter>();

            if (!meshCollider)
                meshCollider = GetComponent<MeshCollider>();

            if (meshCollider && meshFilter)
                meshCollider.sharedMesh = meshFilter.sharedMesh;
        }

        #if UNITY_EDITOR
        private void Save() 
        {
            QcFile.Save.ToAssets(meshFilter.sharedMesh);
        }
        #endif

        #region Inspector
        void IPEGI.Inspect()
        {
            "Mesh Filter".PL().Edit(ref meshFilter).Nl();
            "Mesh Collider".PL().Edit(ref meshCollider).Nl();
            "Width: ".PL().Edit(ref testWidth).Nl();
            "Thickness ".PL().Edit(ref thickness).Nl();
                /*   "UV Sector".PegiLabel().edit(ref uvSector).changes(ref changed);
            if (uvSector != new Vector4(0,0,1,1) && icon.Refresh.Click())
                uvSector = new Vector4(0,0,1,1);
                */

                if (!meshFilter)
                    meshFilter = gameObject.GetComponent<MeshFilter>();

            if (!meshFilter)
            {
                "Add Mesh filter".PL().Click().Nl().OnChanged(() => gameObject.AddComponent<MeshFilter>());
            }
            else
                "Generate".PL().Click(() =>
                {
                    meshFilter.mesh = GenerateMesh(testWidth * 2);

                    if (meshCollider)
                        meshCollider.sharedMesh = meshFilter.sharedMesh;
                });

            #if UNITY_EDITOR
            if (meshFilter && meshFilter.sharedMesh && "Save".PL().Click())
                Save();
#endif



            pegi.Nl();

            "For Pix Art shader set width equal to texture size, and thickness - 0".PL().Write_Hint();

        }
        #endregion

    }


    [PEGI_Inspector_Override(typeof(PixelArtMeshGenerator))] internal class PixelArtMeshGeneratorEditor : PEGI_Inspector_Override { }

}