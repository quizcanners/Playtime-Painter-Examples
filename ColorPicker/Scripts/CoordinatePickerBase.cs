using System;
using QuizCanners.Inspect;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PainterTool.Examples
{

    [ExecuteInEditMode]
    public abstract class CoordinatePickerBase : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler {
        private static CoordinatePickerBase currentPicker;
        
        [NonSerialized] public bool mouseDown;

        protected bool Down { get { return mouseDown; }
            set {

                if (value) {
                    if (currentPicker && currentPicker != this)
                        currentPicker.mouseDown = false;

                    currentPicker = this;
                    mouseDown = true;
                }
                
                mouseDown = value;
      
            }
        }

        [NonSerialized] private Camera clickCamera;
        public RectTransform rectTransform;

        public Vector2 uvClick;
        public abstract bool UpdateFromUV(Vector2 clickUV);
        
        public void OnPointerDown(PointerEventData eventData) => Down = DataUpdate(eventData);

        public void OnPointerUp(PointerEventData eventData) => Down = false;

        public void OnPointerEnter(PointerEventData eventData) {  }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (Down)
                DataUpdate(eventData);
        }

        private bool DataUpdate(PointerEventData eventData)
        {

            if (DataUpdate(eventData.position, eventData.pressEventCamera))
                clickCamera = eventData.pressEventCamera;
            else return false;

            return true;
        }

        private bool DataUpdate(Vector2 position, Camera cam)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, position, cam, out Vector2 localCursor))
                return false;

            pegi.GameView.MouseOverUI = true;

            uvClick = (localCursor / rectTransform.rect.size) + Vector2.one * 0.5f;

            return UpdateFromUV(uvClick);
        }

        protected virtual void Update() {
            if (!Input.GetMouseButton(0) || (Down && !DataUpdate(Input.mousePosition, clickCamera)))
                Down = false;
        }

        protected virtual void OnEnable() {
            if (!rectTransform)
                rectTransform = GetComponent<RectTransform>();
            mouseDown = false;
        }

 
    }
}
