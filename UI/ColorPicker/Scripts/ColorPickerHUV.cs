using System.Collections.Generic;
using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;
using UnityEngine.UI;
using QuizCanners.Lerp;


// Note: Some seamingly arbitraru constants in script and shader are there to match Unity's picker (For Editor UI).

namespace PlaytimePainter.Examples {

    [ExecuteInEditMode]
    public class ColorPickerHUV : CoordinatePickerBase, IPEGI
    {

        public static ColorPickerHUV instance;

        private static float Saturation { get { return ColorPickerContrast.Saturation; } set { ColorPickerContrast.Saturation = value; } }

        private static float Value { get { return ColorPickerContrast.Value; } set { ColorPickerContrast.Value = value; } }
        
        public List<Graphic> graphicToShowBrushColorRGB = new List<Graphic>();
        
        public List<Graphic> graphicToShowBrushColorRGBA = new List<Graphic>();
        
        public static Color lastValue;

        public static float hue;
      
        public static void UpdateBrushColor() {

            Color col = Color.HSVToRGB(hue, Saturation, Value);

            var bc = PainterCamera.Data.Brush;

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

        private static ShaderProperty.FloatValue contrastProperty = new ShaderProperty.FloatValue("_Picker_Contrast");
        private static ShaderProperty.FloatValue brightnessProperty = new ShaderProperty.FloatValue("_Picker_Brightness");
        private static ShaderProperty.FloatValue huvProperty = new ShaderProperty.FloatValue("_Picker_HUV");

        protected override void Update()
        {
            base.Update();

            if (PainterCamera.Data) {
                var col = PainterCamera.Data.Brush.Color;

                if ((!ColorPickerContrast.inst || !ColorPickerContrast.inst.mouseDown) && LerpUtils.DistanceRgba(lastValue, col) >0.002f) {
                    
                    float H;
                    float S;
                    float V;
                    Color.RGBToHSV(col, out H, out S, out V);

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
        
        public void Inspect()
        {
            "HUE: {0}".F(hue).nl();
            "Saturateion: {0}".F(Saturation).nl();
            "Value: {0}".F(Saturation).nl();
        }
    }

    [PEGI_Inspector_Override(typeof(ColorPickerHUV))] internal class ColorPickerHUVEditor : PEGI_Inspector_Override { }

}
