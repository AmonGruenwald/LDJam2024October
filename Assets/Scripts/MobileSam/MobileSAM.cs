using System;
using System.Collections;
using Doji.AI.Segmentation;
using Unity.Sentis;
using UnityEngine;

public class MobileSAM : IDisposable
{
    internal static class Transforms
    {
        public static Tensor ApplyImage(Texture image)
        {
            TextureTransform transform = default(TextureTransform);
            transform.SetTensorLayout(TensorLayout.NHWC);
            var (height, width) = GetPreprocessShape(image.height, image.width, 1024);
            transform.SetDimensions(width, height);
            Tensor<float> tensor = TextureConverter.ToTensor(image, transform);
            tensor.Reshape(tensor.shape.Squeeze(0));
            return tensor;
        }

        public static float[] ApplyCoords(float[] pointCoords, (int height, int width) origSize)
        {
            pointCoords = pointCoords.Copy();
            (int width, int height) preprocessShape = GetPreprocessShape(origSize.height, origSize.width, 1024);
            float num = preprocessShape.width;
            float num2 = preprocessShape.height;
            float num3 = num;
            float num4 = num2;
            for (int i = 0; i < pointCoords.Length; i += 2)
            {
                pointCoords[i] *= num4 / (float)origSize.width;
                pointCoords[i + 1] *= num3 / (float)origSize.height;
            }

            return pointCoords;
        }

        private static (int width, int height) GetPreprocessShape(int oldH, int oldW, int longSideLength)
        {
            float num = (float)longSideLength / (float)Math.Max(oldW, oldH);
            int item = (int)((float)oldW * num + 0.5f);
            int item2 = (int)((float)oldH * num + 0.5f);
            return (item2, item);
        }
    }

    private Model _encoderModel;

    private static ModelAsset _encoderAsset;

    private static Model _decoderModel;

    private ModelAsset _decoderAsset;

    private BackendType _backend = BackendType.CPU;

    private Worker _encoder;

    private Worker _decoder;

    private readonly Tensor[] _inputTensors = new Tensor[6];

    private bool _isImageSet;

    private (int height, int width) _origSize;

    private Vector2Int _inputSize;

    private Tensor _features;

    private const int IMG_SIZE = 1024;

    public Model Encoder
    {
        get
        {
            if (_encoderAsset == null)
            {
                _encoderAsset = Resources.Load<ModelAsset>("ONNX/mobilesam.encoder");
            }

            if (_encoderAsset == null)
            {
                Debug.LogError("MobileSAM encoder ONNX model not found.");
            }

            return _encoderModel ?? (_encoderModel = ModelLoader.Load(_encoderAsset));
        }
    }

    public Model Decoder
    {
        get
        {
            if (_decoderAsset == null)
            {
                _decoderAsset = Resources.Load<ModelAsset>("ONNX/mobilesam.decoder");
            }

            if (_decoderAsset == null)
            {
                Debug.LogError("MobileSAM decoder ONNX model not found.");
            }

            return _decoderModel ?? (_decoderModel = ModelLoader.Load(_decoderAsset));
        }
    }

    public BackendType Backend
    {
        get
        {
            return _backend;
        }
        set
        {
            if (_backend != value)
            {
                DisposeModels();
                _backend = value;
                InitializeNetwork();
            }
        }
    }

    public RenderTextureFormat OutputFormat { get; set; } = RenderTextureFormat.ARGB32;


    public RenderTexture Result { get; private set; }

    public MobileSAM()
    {
        InitializeNetwork();
    }

    private void InitializeNetwork()
    {
        if (Encoder != null && Decoder != null)
        {
            _encoderModel = AddPreprocessing(_encoderModel);
            _encoder = new Worker(Encoder, Backend);
            _decoder = new Worker(Decoder, Backend);
        }
    }

    public IEnumerator SetImage(Texture image)
    {
        (int, int) origSize = (image.height, image.width);
        using Tensor transformedImage = Transforms.ApplyImage(image);
        yield return SetImage(transformedImage, origSize);
        if (Result != null)
        {
            Result.Release();
        }

        Result = new RenderTexture(image.width, image.height, 0, OutputFormat);
    }

    public IEnumerator SetImage(Tensor transformedImage, (int height, int width) origSize)
    {
        Debug.Assert(Math.Max(transformedImage.shape[0], transformedImage.shape[1]) == 1024, "Image must have a long side of {IMG_SIZE}}.");
        ResetImage();
        _origSize = origSize;
        _inputSize = new Vector2Int(transformedImage.shape[-2], transformedImage.shape[-1]);
        var iter = _encoder.ScheduleIterable(transformedImage);

        int i = 0;
        while (iter.MoveNext())
        {
            if (i % 30 == 0) {
                yield return null;
            }
            i++;
        }

        _features = _encoder.PeekOutput();
        _isImageSet = true;
    }

    private void ResetImage()
    {
        _isImageSet = false;
        _features = null;
        _origSize = default((int, int));
        _inputSize = default(Vector2Int);
    }

    public void Predict(float[] pointCoords, float[] pointLabels)
    {
        if (!_isImageSet)
        {
            throw new InvalidOperationException("An image must be set with .SetImage(...) before mask prediction.");
        }

        if (pointCoords == null)
        {
            throw new ArgumentNullException("pointCoords can not be null.", "pointCoords");
        }

        if (pointLabels == null)
        {
            throw new ArgumentNullException("pointLabels can not be null.", "pointLabels");
        }

        int num = ((pointCoords != null) ? (pointCoords.Length / 2) : 0);
        int num2 = ((pointLabels != null) ? pointLabels.Length : 0);
        if (num != num2)
        {
            throw new ArgumentException("number of point labels does not match the number of points.");
        }

        pointCoords = Transforms.ApplyCoords(pointCoords, _origSize);
        using Tensor<float> pointCoords2 = new Tensor<float>(new TensorShape(1, num, 2), pointCoords);
        using Tensor<float> pointLabels2 = new Tensor<float>(new TensorShape(1, num), pointLabels);
        using Tensor<float> maskInput = new Tensor<float>(new TensorShape(1, 1, 256, 256));
        using Tensor<float> hasMaskInput = new Tensor<float>(new TensorShape(1), new float[1]);
        using Tensor<float> origImSize = new Tensor<float>(new TensorShape(2), new float[2] { _origSize.height, _origSize.width });
        TextureConverter.RenderToTexture(Predict(pointCoords2, pointLabels2, maskInput, hasMaskInput, origImSize).Masks, Result, default(TextureTransform).SetBroadcastChannels(broadcastChannels: true));
    }

    public IEnumerator PredictAsync(float[] pointCoords, float[] pointLabels)
    {
        if (!_isImageSet)
        {
            throw new InvalidOperationException("An image must be set with .SetImage(...) before mask prediction.");
        }

        if (pointCoords == null)
        {
            throw new ArgumentNullException("pointCoords can not be null.", "pointCoords");
        }

        if (pointLabels == null)
        {
            throw new ArgumentNullException("pointLabels can not be null.", "pointLabels");
        }

        int num = ((pointCoords != null) ? (pointCoords.Length / 2) : 0);
        int num2 = ((pointLabels != null) ? pointLabels.Length : 0);
        if (num != num2)
        {
            throw new ArgumentException("number of point labels does not match the number of points.");
        }

        pointCoords = Transforms.ApplyCoords(pointCoords, _origSize);
        using Tensor<float> pointCoords2 = new Tensor<float>(new TensorShape(1, num, 2), pointCoords);
        using Tensor<float> pointLabels2 = new Tensor<float>(new TensorShape(1, num), pointLabels);
        using Tensor<float> maskInput = new Tensor<float>(new TensorShape(1, 1, 256, 256));
        using Tensor<float> hasMaskInput = new Tensor<float>(new TensorShape(1), new float[1]);
        using Tensor<float> origImSize = new Tensor<float>(new TensorShape(2), new float[2] { _origSize.height, _origSize.width });
        yield return PredictAsync(pointCoords2, pointLabels2, maskInput, hasMaskInput, origImSize);
        TextureConverter.RenderToTexture(lastDecoderOutput.Masks, Result, default(TextureTransform).SetBroadcastChannels(broadcastChannels: true));
    }

    DecoderOutput lastDecoderOutput;

    private DecoderOutput Predict(Tensor pointCoords, Tensor pointLabels, Tensor maskInput, Tensor hasMaskInput, Tensor origImSize)
    {
        _inputTensors[0] = _features;
        _inputTensors[1] = pointCoords;
        _inputTensors[2] = pointLabels;
        _inputTensors[3] = maskInput;
        _inputTensors[4] = hasMaskInput;
        _inputTensors[5] = origImSize;

        _decoder.Schedule(_inputTensors);
        Tensor lowResMasks = _decoder.PeekOutput("low_res_masks");
        Tensor iouPredictions = _decoder.PeekOutput("iou_predictions");
        Tensor masks = _decoder.PeekOutput("masks");

        return new DecoderOutput(lowResMasks, iouPredictions, masks);
    }

    public IEnumerator PredictAsync(Tensor pointCoords, Tensor pointLabels, Tensor maskInput, Tensor hasMaskInput, Tensor origImSize)
    {
        _inputTensors[0] = _features;
        _inputTensors[1] = pointCoords;
        _inputTensors[2] = pointLabels;
        _inputTensors[3] = maskInput;
        _inputTensors[4] = hasMaskInput;
        _inputTensors[5] = origImSize;

        var iter = _decoder.ScheduleIterable(_inputTensors);

        int i = 0;
        while (iter.MoveNext())
        {
            if (i < 150) {
                if (i % 12 == 0) {
                    yield return null;
                }
            } else {
                if (i % 3 == 0) {
                    yield return null;
                }
            }
            i++;
        }

        Tensor lowResMasks = _decoder.PeekOutput("low_res_masks");
        Tensor iouPredictions = _decoder.PeekOutput("iou_predictions");
        Tensor masks = _decoder.PeekOutput("masks");

        lastDecoderOutput = new DecoderOutput(lowResMasks, iouPredictions, masks);
    }

    private Model AddPreprocessing(Model model)
    {
        FunctionalGraph functionalGraph = new FunctionalGraph();
        FunctionalTensor[] array = functionalGraph.AddInputs(model);
        FunctionalTensor[] array2 = array;
        array2[0] *= 255f;
        FunctionalTensor[] outputs = Functional.Forward(model, array);
        return functionalGraph.Compile(outputs);
    }

    public void Dispose()
    {
        DisposeModels();
        Result.Release();
    }

    private void DisposeModels()
    {
        _encoder?.Dispose();
        _decoder?.Dispose();
    }
}

internal static class ArrayUtils {
    public static float[] Copy(this float[] src) {
        float[] target = new float[src.Length];
        src.CopyTo(target, 0);
        return target;
    }
}