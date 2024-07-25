using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetSkyColors : MonoBehaviour {
    public Color skyColor1;
    public Color skyColor2;
    public Color skyColor3;
    public Color skyColor4;
    public Color lightColor = Color.white;
    public Color sunColor = Color.white;
    public float fogLevel = 0.0f;
    public bool is5D = false;

    void Awake() {
        if (is5D) {
            Vector5 lightDir = new Vector5(0.5f, -0.7f, 0.4f, 0.1f, 0.3f);
            Shader.SetGlobalVector("_LightDirA", (Vector4)lightDir);
            Shader.SetGlobalFloat("_LightDirV", lightDir.v);
        } else {
            Vector4 lightDir = new Vector4(2, -4, -3, 1).normalized;
            Shader.SetGlobalVector("_LightDirA", lightDir);
        }
        Color cameraBackgroundColor = lightColor;
        BasicCamera4D.sliceBackgroundColor = cameraBackgroundColor;
        BasicCamera5D.sliceBackgroundColor = cameraBackgroundColor;
        Shader.SetGlobalColor("_LightCol", cameraBackgroundColor);
        Shader.SetGlobalColor("_SunColor", sunColor);
        Shader.SetGlobalColor("_SkyColor1", skyColor1);
        Shader.SetGlobalColor("_SkyColor2", skyColor2);
        Shader.SetGlobalColor("_SkyColor3", skyColor3);
        Shader.SetGlobalColor("_SkyColor4", skyColor4);
    }
}
