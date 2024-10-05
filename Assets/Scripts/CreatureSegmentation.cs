using System;
using Doji.AI.Segmentation;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class CreatureSegmentation : MonoBehaviour
{
    private InputAction touchAction;
    [SerializeField] private ARCameraManager cameraManager;
    [SerializeField] private RawImage previewImage;

    private MobileSAM segmentation;
    

    private void Awake()
    {

        segmentation = new MobileSAM();
        segmentation.Backend = Unity.Sentis.BackendType.CPU;
    }

    public Texture2D TakeSnapshot()
    {
        var texture = ScreenCapture.CaptureScreenshotAsTexture();
        var textureNoAlpha = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
        var pixels = texture.GetPixels32();
        textureNoAlpha.SetPixels32(pixels);
        textureNoAlpha.Apply();
        Destroy(texture);
        
        return textureNoAlpha;
    }

    public Texture2D SegmentTexture(Texture2D texture, Vector2 tappedPointNormalized)
    {
        Vector2 tappedPointTextureSpace = new Vector2(tappedPointNormalized.x * texture.width, tappedPointNormalized.y * texture.height);
        Debug.Log("point in texture space: " + tappedPointTextureSpace + ", width: " + texture.width + ", height: " + texture.height);
        segmentation.SetImage(texture);

        float[] pointCoords = new float[] { tappedPointTextureSpace.x, tappedPointTextureSpace.y };
        float[] pointLabels = new float[] { 1f };  // 1 for foreground point
        segmentation.Predict(pointCoords, pointLabels);
        
        var segmentationTextureRt = segmentation.Result;
        
        RenderTexture currentActiveRT = RenderTexture.active;
        RenderTexture.active = segmentationTextureRt;

        Texture2D segmentationTexture = new Texture2D(segmentationTextureRt.width, segmentationTextureRt.height, TextureFormat.RGBA32, false);
        segmentationTexture.ReadPixels(new Rect(0, 0, segmentationTexture.width, segmentationTexture.height), 0, 0);
        segmentationTexture.Apply();

        RenderTexture.active = currentActiveRT;

        Texture2D extracted = ExtractTexture(texture, segmentationTexture);
        Rect bounds = GetTextureAlphaBoundingBox(segmentationTexture); // ATTENTION: might have size zero, add error handling
        if (bounds.width == 0 || bounds.height == 0) {
            return null;
        }

        Texture2D cropped = CropTexture(extracted, (int) bounds.min.x, (int) bounds.min.y, (int) bounds.width, (int) bounds.height);
        Destroy(extracted);
        // previewImage.texture = cropped;

        return cropped;
    }

    public Texture2D CropTexture(Texture2D sourceTexture, int x, int y, int width, int height)
    {
        if (x < 0 || y < 0 || x + width > sourceTexture.width || y + height > sourceTexture.height)
        {
            Debug.LogError("Crop area is out of bounds of the source texture.");
            return null;
        }

        Color[] pixels = sourceTexture.GetPixels(x, y, width, height);

        Texture2D croppedTexture = new Texture2D(width, height);
        croppedTexture.SetPixels(pixels);
        croppedTexture.Apply();

        return croppedTexture;
    }

    private Rect GetTextureAlphaBoundingBox(Texture2D tex)
    {
        Color32[] pixels = tex.GetPixels32();
        int width = tex.width;
        int height = tex.height;

        int minX = width;
        int minY = height;
        int maxX = 0;
        int maxY = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = x + y * width;
                if (pixels[index].a > 128)
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }

        if (minX > maxX || minY > maxY)
            return new Rect(0, 0, 0, 0);

        return new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    private Texture2D ExtractTexture(Texture2D imageTexture, Texture2D segmentationTexture)
    {
        Texture2D extractedTexture = new Texture2D(imageTexture.width, imageTexture.height, TextureFormat.RGBA32, false);

        Color32[] imagePixels = imageTexture.GetPixels32();
        Color32[] segmentationPixels = segmentationTexture.GetPixels32();
        int width = imageTexture.width;
        int height = imageTexture.height;
        Color32[] newPixels = new Color32[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int indexColor = x + y * width;
                float normalizedX = ((float) x) / ((float) width);
                float normalizedY = ((float) y) / ((float) height);

                int xInSegmentationSpace = (int) (normalizedX * ((float) segmentationTexture.width));
                int yInSegmentationSpace = (int) (normalizedY * ((float) segmentationTexture.height));

                int indexSegment = xInSegmentationSpace + yInSegmentationSpace * segmentationTexture.width;
                var segmentColor = segmentationPixels[indexSegment];

                if (segmentColor.r > 128) {
                    newPixels[indexColor] = imagePixels[indexColor];
                } else {
                    newPixels[indexColor] = new Color32(0, 0, 0, 0);
                }
            }
        }

        extractedTexture.SetPixels32(newPixels);
        extractedTexture.Apply();

        return extractedTexture;
    }

    private void OnDestroy()
    {
    }
}
