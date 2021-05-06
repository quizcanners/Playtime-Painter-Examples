using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;

public class InfiniteParticlesDrawerGUI : PEGI_Inspector_Material {

    public const string FadeOutTag = "_FADEOUT";
     
    public override bool Inspect(Material mat) {

        var changed = pegi.toggleDefaultInspector(mat);

        mat.toggle("SCREENSPACE").nl();
        mat.toggle("DYNAMIC_SPEED").nl();
        mat.toggle(FadeOutTag).nl();

        var fo = mat.HasTag(FadeOutTag);

        if (fo)
            "When alpha is one, the graphic will be invisible.".writeHint();

        pegi.nl();

        var dynamicSpeed = mat.GetKeyword("DYNAMIC_SPEED");

        pegi.nl();
        
        if (!dynamicSpeed)
            mat.edit(speed, "speed", 0, 60).nl();
        else
        {
            mat.edit(time, "Time").nl();
            "It is expected that time Float will be set via script. Parameter name is _CustomTime. ".writeHint();
            pegi.nl();
        }

        mat.edit(tiling, "Tiling", 0.1f, 20f).nl();

        mat.edit(upscale, "Scale", 0.1f, 1).nl();

        mat.editTexture("_MainTex").nl();
        mat.editTexture("_MainTex2").nl();


        return changed;
    }
    
    private static readonly ShaderProperty.FloatValue speed = new ShaderProperty.FloatValue("_Speed");
    private static readonly ShaderProperty.FloatValue time = new ShaderProperty.FloatValue("_CustomTime");
    private static readonly ShaderProperty.FloatValue tiling = new ShaderProperty.FloatValue("_Tiling");
    private static readonly ShaderProperty.FloatValue upscale = new ShaderProperty.FloatValue("_Upscale");
    
}

