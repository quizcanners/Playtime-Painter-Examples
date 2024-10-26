using System.Collections.Generic;
using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;
using UnityEngine.UI;
using QuizCanners.Lerp;


// Note: Some seamingly arbitraru constants in script and shader are there to match Unity's picker (For Editor UI).

namespace PainterTool.Examples {

    [ExecuteInEditMode]
    public class ColorPickerHUV : CoordinatePickerBase, IPEGI
    {

        public static ColorPickerHUV instance;

        private static float Saturation { get { return ColorPickerContrast.Saturation; } set { ColorPickerContrast.Saturation = value; } }

        private static float Value { get { return ColorPickerContrast.Value; } set { ColorPickerContrast.Value = value; } }
        
        public List<Graphic> graphicToShowBrushColorRGB = new();
        
        public List<Graphic> graphicToShowBrushColorRGBA = new();
        
        public static Color lastValue;

        public static float hue;
      
        public static void UpdateBrushColor() {

            Color col = Color.HSVToRGB(hue, Saturation, Value);

            var bc = Painter.Data.Brush;

            col.a = bc.Color.a;

            bc.Color = col;
            
            lastValue = col;

            contrastProperty.GlobalValue = ColorPickerContrast.Saturation;
            brightnessProperty.GlobalValue = ColorPickerContrast.Value;
            huvProperty.GlobalValue = hue;

            if (instance) {
                instance.graphicToShowBrushColorRGB.TrySetColor_RGB(col);
                col.a *= col.a;
                instance.graphicToShowBrushColorRGBA.TrySetColor_RGBA(col);
            }
        }

        private static readonly ShaderProperty.FloatValue contrastProperty = new("_Picker_Contrast");
        private static readonly ShaderProperty.FloatValue brightnessProperty = new("_Picker_Brightness");
        private static readonly ShaderProperty.FloatValue huvProperty = new("_Picker_HUV");

        protected override void Update()
        {
            base.Update();

            if (Painter.Data) 
            {
                var col = Painter.Data.Brush.Color;

                if ((!ColorPickerContrast.inst || !ColorPickerContrast.inst.mouseDown) && QcLerp.DistanceRgba(lastValue, col) >0.002f) {

                    Color.RGBToHSV(col, out float H, out float S, out float V);

                    if (!float.IsNaN(H)) {
                        hue = H;
                        Saturation = S;
                        Value = V;

                        UpdateBrushColor();
                    }
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            instance = this;

        }

        public override bool UpdateFromUV(Vector2 clickUV) {
            var tmp = (((clickUV.YX() - 0.5f * Vector2.one).Angle() + 360) % 360) / 360f;

            if (!float.IsNaN(tmp)) {
                hue = tmp;
                UpdateBrushColor();
            }
            return true;
        }
        
        void IPEGI.Inspect()
        {
            "HUE: {0}".F(hue).PegiLabel().Nl();
            "Saturateion: {0}".F(Saturation).PegiLabel().Nl();
            "Value: {0}".F(Saturation).PegiLabel().Nl();
        }
    }

    [PEGI_Inspector_Override(typeof(ColorPickerHUV))] internal class ColorPickerHUVEditor : PEGI_Inspector_Override { }

}
