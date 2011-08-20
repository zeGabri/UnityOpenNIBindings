#define RENDER_WITH_OPENGL

using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using OpenNI;
using Tao.OpenGl;

public class OpenNIImagemapViewer : MonoBehaviour 
{
    public Renderer target;
	private ImageGenerator Image;
	private Texture2D imageMapTexture;
	private int imageLastFrameId;
    byte[] rawImageMap;
    Color32[] imageMapPixels;

    bool openGl;

	// Use this for initialization
	void Start () 
	{
        if (null == target) {
            target = renderer;
        }

		Image = OpenNIContext.OpenNode(NodeType.Image) as ImageGenerator;
		imageMapTexture = new Texture2D( Image.MapOutputMode.XRes,  Image.MapOutputMode.YRes, TextureFormat.RGB24, false);
        rawImageMap = new byte[Image.GetMetaData().BytesPerPixel * Image.MapOutputMode.XRes * Image.MapOutputMode.YRes];
        imageMapPixels = new Color32[Image.MapOutputMode.XRes * Image.MapOutputMode.YRes];

        if (null != target) {
            // rendering to a mesh means the texture must be POT
            imageMapTexture = new Texture2D(Mathf.NextPowerOfTwo(Image.MapOutputMode.XRes), Mathf.NextPowerOfTwo(Image.MapOutputMode.YRes), TextureFormat.RGB24, false);
		    float uScale =  Image.MapOutputMode.XRes / (float)Mathf.NextPowerOfTwo(Image.MapOutputMode.XRes);
            float vScale = Image.MapOutputMode.YRes / (float)Mathf.NextPowerOfTwo(Image.MapOutputMode.YRes); 
            target.material.SetTextureScale("_MainTex", new Vector2(uScale, -vScale));
            target.material.SetTextureOffset("_MainTex", new Vector2(0.0f, vScale - 1.0f));
            target.material.mainTexture = imageMapTexture;
        }

        openGl = SystemInfo.graphicsDeviceVersion.Contains("OpenGL");
        if (openGl) {
            print("Running in OpenGL mode");
        }
	}
	
	void FixedUpdate () {
		// see if we have a new RGB frame
		if (Image.FrameID != imageLastFrameId) {
			imageLastFrameId = Image.FrameID;
			WriteImageToTexture(imageMapTexture);
		}
	}

    void WriteImageToTexture(Texture2D tex)
    {
        // NOTE: This method only works when unity is rendering with OpenGL ("unity.exe -force-opengl"). This is *much* faster
        // then Texture2D::SetPixels32 which we would have to use otherwise
        // NOTE: The native texture id needs a +1 if we are rendering to GUI
        if (openGl) {
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, (null == target) ? tex.GetNativeTextureID() + 1 : tex.GetNativeTextureID());
            Gl.glTexSubImage2D(Gl.GL_TEXTURE_2D, 0, 0, 0, Image.MapOutputMode.XRes, Image.MapOutputMode.YRes, Gl.GL_RGB, Gl.GL_UNSIGNED_BYTE, Image.ImageMapPtr);
            return;
        }

        Marshal.Copy(Image.ImageMapPtr, rawImageMap, 0, rawImageMap.Length);
        int src = 0;
        for (int i = 0; i < Image.MapOutputMode.XRes * Image.MapOutputMode.YRes; i++, src += 3) {
            new Color32(rawImageMap[src], rawImageMap[src + 1], rawImageMap[src + 2], 255);
        }
        tex.SetPixels32(imageMapPixels);
        tex.Apply();
    }

   void OnGUI()
   {
       if (null == target) {
           GUI.Box(new Rect(Screen.width - 128, Screen.height - 96, 128, 96), imageMapTexture);
       }
   }
}