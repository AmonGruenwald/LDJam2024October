using Unity.Sentis;
using FF = Unity.Sentis.Functional;

using UnityEngine;
using UnityEngine.UI;



public class CreatureClassification : MonoBehaviour
{
    public ModelAsset modelAsset;
    public TextAsset labelsAsset;

    private string[] labels;

    private Worker worker;

    public ClassificationPrediction Prediction;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        labels = labelsAsset.text.Split('\n');
        var model = ModelLoader.Load(modelAsset);

        worker = new Worker(model, BackendType.CPU);
    }

    public void Classify(Texture2D texture)
    {
        int size = 224;
        var resized = ResizeTexture(texture, size, size);

        Color[] pixels = resized.GetPixels(0, 0, size, size);
        float[] floats = new float[size * size * 3];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                var color = pixels[y * size + x];
                floats[0 * size * size + y * size + x] = (color.r - 0.485f) / 0.229f; // Channel 0
                floats[1 * size * size + y * size + x] = (color.g - 0.456f) / 0.224f; // Channel 1
                floats[2 * size * size + y * size + x] = (color.b - 0.406f) / 0.225f; // Channel 2
            }
        }

        Tensor input = new Tensor<float>(new TensorShape(1, 3, 224, 224), floats);
        worker.Schedule(input);

        var output_tmp = worker.PeekOutput();
        var output = output_tmp.ReadbackAndClone() as Tensor<float>;

        int bestClass = 0;
        float maxProbability = output[0];
        for (int i = 1; i < output.count; i++)
        {
            if (output[i] > maxProbability)
            {
                maxProbability = output[i];
                bestClass = i;
            }
        }

        int percent = Mathf.FloorToInt(maxProbability * 100f + 0.5f);

        // Log the prediction
        Prediction = new ClassificationPrediction {
            categoryIndex = bestClass,
            categoryName = labels[bestClass],
            confidenceInPercent = percent,
        };
        Debug.Log($"Prediction: {labels[bestClass]} {percent}%");

        // Clean up
        input.Dispose();
        output.Dispose();

        Destroy(resized);
    }

    public static Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
    {
        // Create a new Texture2D with the specified size
        Texture2D resizedTexture = new Texture2D(newWidth, newHeight, TextureFormat.RGB24, false);
        
        // Scale the pixels using bilinear sampling
        for (int y = 0; y < newHeight; y++)
        {
            for (int x = 0; x < newWidth; x++)
            {
                // Calculate the sample position
                float u = x / (float)newWidth;
                float v = y / (float)newHeight;
                
                // Sample the color at the calculated position
                Color color = source.GetPixelBilinear(u, v);
                
                // Set the pixel in the new texture
                resizedTexture.SetPixel(x, y, color);
            }
        }

        // Apply changes to the texture
        resizedTexture.Apply();
        return resizedTexture;
    }
}

public class ClassificationPrediction {
    public int categoryIndex;
    public string categoryName;
    public float confidenceInPercent;
}
