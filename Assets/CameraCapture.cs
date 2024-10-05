using UnityEngine;
using UnityEngine.Rendering;

public class CameraCapture : MonoBehaviour
{
    private Camera _cam;
    private RenderTexture _tex;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        _cam = GetComponent<Camera>();

        var texture = new RenderTexture(_cam.pixelWidth, _cam.pixelHeight, 32);
        _cam.targetTexture = texture;

        _tex = texture;
    }

    private void Update() {
        CommandBuffer cmdBuffer = CommandBufferPool.Get();
        RenderTargetIdentifier rti = new RenderTargetIdentifier(_tex);
        cmdBuffer.Blit(BuiltinRenderTextureType.CameraTarget, rti);
        _cam.AddCommandBuffer(CameraEvent.AfterEverything, cmdBuffer);
    }
}
