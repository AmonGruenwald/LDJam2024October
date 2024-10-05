using System.Collections.Generic;
using Unity.Sentis;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class YoloTest : MonoBehaviour
{
    public ModelAsset modelAsset;
    public TextAsset labelsAsset;
    // Create a Raw Image in the scene and link it here:
    public RawImage displayImage;
    // Link to a bounding box texture here:
    public Sprite borderSprite;
    public Texture2D borderTexture;
    // Link to the font for the labels:
    public Font font;

    private Transform displayLocation;
    private Model model;
    private Worker worker;
    private string[] labels;
    const BackendType backend = BackendType.GPUCompute;

    //Image size for the model
    private const int imageWidth = 640;
    private const int imageHeight = 640;

    private VideoPlayer video;

    List<GameObject> boxPool = new List<GameObject>();
    //bounding box data
    public struct BoundingBox
    {
        public float centerX;
        public float centerY;
        public float width;
        public float height;
        public string label;
        public float confidence;
    }

    void Start()
    {
        Application.targetFrameRate = 60;
        Screen.orientation = ScreenOrientation.LandscapeLeft;

        //Parse neural net labels
        labels = labelsAsset.text.Split('\n');

        //Load model
        model = ModelLoader.Load(modelAsset);
        //model = ModelLoader.Load(Application.streamingAssetsPath + "/" + modelName);
        //Create image to display video
        displayLocation = displayImage.transform;

        //Create engine to run model
        worker = new Worker(model, backend);

        if (borderSprite == null)
        {
            borderSprite = Sprite.Create(borderTexture, new Rect(0, 0, borderTexture.width, borderTexture.height), new Vector2(borderTexture.width / 2, borderTexture.height / 2));
        }
    }

    private void Update()
    {
        ExecuteML();
    }

    public void ExecuteML()
    {
        Debug.Log("ExecuteML");

        ClearAnnotations();

        using var input = TextureConverter.ToTensor(displayImage.texture, imageWidth, imageHeight, 3);
        worker.Schedule(input);

        //Read output tensors
        var outputTemp = worker.PeekOutput() as Tensor<float>;
        var output = outputTemp.ReadbackAndClone();

        float displayWidth = displayImage.rectTransform.rect.width;
        float displayHeight = displayImage.rectTransform.rect.height;

        float scaleX = displayWidth / imageWidth;
        float scaleY = displayHeight / imageHeight;

        int foundBoxes = output.shape[0];

        Debug.Log("Found boxes: " + foundBoxes);

        //Draw the bounding boxes
        for (int n = 0; n < foundBoxes; n++)
        {
            float centerX = ((output[n, 1] + output[n, 3]) * scaleX - displayWidth) / 2;
            float centerY = ((output[n, 2] + output[n, 4]) * scaleY - displayHeight) / 2;
            float width = (output[n, 3] - output[n, 1]) * scaleX;
            float height = (output[n, 4] - output[n, 2]) * scaleY;
            Debug.Log("Output label: " + output[n, 5]);
            string label = labels[(int)output[n, 5]];
            float confidence = Mathf.FloorToInt(output[n, 6] * 100 + 0.5f);

            var box = new BoundingBox
            {
                centerX = centerX,
                centerY = centerY,
                width = width,
                height = height,
                label = label,
                confidence = confidence
            };
            DrawBox(box, n);

            Debug.Log("Found box " + n);
        }
    }

    public void DrawBox(BoundingBox box, int id)
    {
        //Create the bounding box graphic or get from pool
        GameObject panel;
        if (id < boxPool.Count)
        {
            panel = boxPool[id];
            panel.SetActive(true);
        }
        else
        {
            panel = CreateNewBox(Color.yellow);
        }
        //Set box position
        panel.transform.localPosition = new Vector3(box.centerX, -box.centerY);

        //Set box size
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(box.width, box.height);

        //Set label text
        var label = panel.GetComponentInChildren<Text>();
        label.text = box.label + " (" + box.confidence + "%)";
    }

    public GameObject CreateNewBox(Color color)
    {
        //Create the box and set image

        var panel = new GameObject("ObjectBox");
        panel.AddComponent<CanvasRenderer>();
        Image img = panel.AddComponent<Image>();
        img.color = color;
        img.sprite = borderSprite;
        img.type = Image.Type.Sliced;
        panel.transform.SetParent(displayLocation, false);

        //Create the label

        var text = new GameObject("ObjectLabel");
        text.AddComponent<CanvasRenderer>();
        text.transform.SetParent(panel.transform, false);
        Text txt = text.AddComponent<Text>();
        txt.font = font;
        txt.color = color;
        txt.fontSize = 40;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;

        RectTransform rt2 = text.GetComponent<RectTransform>();
        rt2.offsetMin = new Vector2(20, rt2.offsetMin.y);
        rt2.offsetMax = new Vector2(0, rt2.offsetMax.y);
        rt2.offsetMin = new Vector2(rt2.offsetMin.x, 0);
        rt2.offsetMax = new Vector2(rt2.offsetMax.x, 30);
        rt2.anchorMin = new Vector2(0, 0);
        rt2.anchorMax = new Vector2(1, 1);

        boxPool.Add(panel);
        return panel;
    }

    public void ClearAnnotations()
    {
        foreach (var box in boxPool)
        {
            box.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        worker?.Dispose();
    }
}