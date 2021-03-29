using System.Collections;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using UnityEngine;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PlaytimePainter
{

    [ExecuteInEditMode]
    public class LightCaster : MonoBehaviour, IPEGI , IGotIndex, IGotName, IPEGI_ListInspect {

        public static readonly Countless<LightCaster> AllProbes = new Countless<LightCaster>();
        private static int freeIndex;

        public ProjectorCameraConfiguration cameraConfiguration;

        Vector3 CameraRootPositionOffset => -transform.forward * cameraConfiguration.nearPlane;

        public ProjectorCameraConfiguration UpdateAndGetCameraConfiguration() {

            cameraConfiguration.CopyTransform(transform);

            cameraConfiguration.position += CameraRootPositionOffset;

            if (camShake > 0)
                cameraConfiguration.rotation = Quaternion.Lerp(cameraConfiguration.rotation, Random.rotation, camShake);
            
            return cameraConfiguration;

        }

        public Color ecol = Color.yellow;
        public float brightness = 1;
        public float camShake = 0.0001f;

        public int index;

        public int IndexForPEGI { get { return index;  } set { index = value; } }
        public string NameForPEGI { get { return gameObject.name; } set { gameObject.name = value; } }

        public void SetChannelIndex(int ind)
        {

           // Debug.Log("Setting mesh color "+ind);

          /*  if (!emissiveMesh)
                return;

            bool valid = ind >= 0 && ind <= 3;

            emissiveMesh.colorAlpha = valid ? 1 : 0;
            emissiveMesh.changeColor = true;

            switch (ind) {
                case 0: emissiveMesh.color = Color.red;
                    break;
                case 1:
                    emissiveMesh.color = Color.green;
                    break;
                case 2:
                    emissiveMesh.color = Color.blue;
                    break;
            }*/

        }

        protected void OnAwake()
        {
            AllProbes[index] = this;
        }

        private void OnEnable() {

            if (cameraConfiguration == null)
                cameraConfiguration = new ProjectorCameraConfiguration();

            if (AllProbes[index]) {
                while (AllProbes[freeIndex]) freeIndex++;
                index = freeIndex;
            }

            AllProbes[index] = this;
        }

        void OnDrawGizmosSelected() {

            Gizmos.color = ecol;
            //Gizmos.DrawWireSphere(transform.position, 1);

            var off = CameraRootPositionOffset;

            transform.position += off;

            Gizmos.matrix = transform.localToWorldMatrix;

            transform.position -= off;

            cameraConfiguration.DrawFrustrum(transform.localToWorldMatrix);
        }

        private void OnDisable() {
            if (AllProbes[index] == this)
                AllProbes[index] = null;
        }

        private void ChangeIndexTo (int newIndex) {
            if (AllProbes[index] == this)
                AllProbes[index] = null;
            index = newIndex;

            if (AllProbes[index])
                Debug.Log("More then one probe is sharing index {0}".F(index));

            AllProbes[index] = this;
        }
        
        private int inspectedElement = -1;

        public void Inspect()
        {
            var changes = pegi.ChangeTrackStart();

            if (inspectedElement == -1)
            {

                var tmp = index;
                if ("Index".edit(ref tmp).nl())
                    ChangeIndexTo(tmp);

                "Shake".edit(50, ref camShake, 0, 0.001f).nl();

                "Emission Color".edit(ref ecol).nl();
                "Brightness".edit(ref brightness).nl();
              //  if (!emissiveMesh)
                //    "Emissive Mesh".edit(ref emissiveMesh).nl(ref changed);
            }

            if ("Projection".enter(ref inspectedElement, 1).nl())
            {
                cameraConfiguration.Nested_Inspect().nl();
            }

            if (inspectedElement == -1 && cameraConfiguration.nearPlane < 5 && "Increase near plane to 5".Click())
                cameraConfiguration.nearPlane = 5;


            if (changes) 
                QcUnity.RepaintViews();
        }

        public void InspectInList(IList list, int ind, ref int edited)
        {
           index.ToString().write(25);
           pegi.edit(ref ecol, 40);

           if (icon.Enter.Click("Inspect"))
               edited = ind;
        }
     

    }

#if UNITY_EDITOR
    [CustomEditor(typeof(LightCaster))]
    public class BakedShadowsLightProbeEditor : PEGI_Inspector_Mono<LightCaster> { }
#endif

}