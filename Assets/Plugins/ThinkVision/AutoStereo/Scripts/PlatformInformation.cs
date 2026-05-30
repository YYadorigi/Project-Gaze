using System;
using UnityEngine;

namespace AS3DPlugin
{
    /// <summary>
    /// Get and maintain as3d platform informations
    /// </summary>
    public class PlatformInformation
    {
        public float screenSizeInInch;
        public float pixelsizeScreen_mm;
        public float screenAspectRatio;
        public bool portrait;
        public int doubleWidthFlag = 1;
        //public float physicalSizeHeight;
        //public float physicalSizeWidth;
        //public float dotPitch;
        public void InitPlatformInformation(StereoCam.AutoStereoShader shader, float screenSizeInInchRaw)
        {
            screenSizeInInch = screenSizeInInchRaw;
            float w, h;
#if UNITY_EDITOR
            w = Screen.width;
            h = Screen.height;
#else
            w = Screen.currentResolution.width;
            h = Screen.currentResolution.height;
#endif
            screenAspectRatio = w / h;
            if (screenAspectRatio < 1)
                portrait = true;
            switch (shader)
            {
                case StereoCam.AutoStereoShader.SideBySide:
                    if (screenSizeInInch == 65 && screenAspectRatio > 3) 
                        doubleWidthFlag = 2;                        
                    pixelsizeScreen_mm = (float)((screenSizeInInch * 25.4) / Math.Sqrt(w / doubleWidthFlag * w / doubleWidthFlag + h * h));
                    break;
                default:
                    pixelsizeScreen_mm = (float)((screenSizeInInch * 25.4) / Math.Sqrt(w * w + h * h));
                    break;
            }
        }
    }
}
