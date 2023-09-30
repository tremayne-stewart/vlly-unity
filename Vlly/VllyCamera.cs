using System.IO;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using System.Diagnostics;
using UnityEngine.Rendering;

namespace vlly {
  [RequireComponent(typeof(Camera))]
  public class VllyCamera : MonoBehaviour {
    private System.Diagnostics.Stopwatch _frameTimer;
    private bool _isCapturing = false;
    private int _frameCount = 0;
    private int _lastFrameId = 0;
    private string _triggerKey;
    private string _triggerId;
    private Camera  _camera;
    private RenderTexture _renderTexture;
    public int RecordedFrameCount {
      get {
        return _frameCount;
      }
    }
    public int RealTimeFrameId {
      get {
        if (_frameTimer == null){
          return 0;
        }
        return (int) (_frameTimer.ElapsedMilliseconds / 1000.0m * Config.RecordingFPS);
      }
    }

    #region Singleton

    #endregion

    #region API
    public bool StartRecording (string triggerKey, string triggerId) {
      if (_isCapturing) {
        return false;
      }
      _triggerKey = triggerKey;
      _triggerId = triggerId;
      _frameCount = 0;
      _lastFrameId = -1;
      _frameTimer = new Stopwatch();
      _frameTimer.Start();
      _isCapturing = true;
      return true;
    }

    public bool StopRecording() {
      if (!_isCapturing) {
        return false;;
      }
      _frameTimer.Stop();
      return true;
    }
    #endregion

    public void Start() {
      RenderPipelineManager.endFrameRendering += OnEndFrameRendering;
    }
    void OnDestroy() {
        RenderPipelineManager.endFrameRendering -= OnEndFrameRendering;
    }
    public void Update() {
      if (!_camera) {
        _camera = this.GetComponent<Camera>();
        _renderTexture = new RenderTexture(
          (int)_camera.pixelWidth, 
          (int)_camera.pixelHeight, 
          24, RenderTextureFormat.ARGB32);
        _renderTexture.Create();
        _camera.targetTexture = _renderTexture;
      }
    }

    #region Capture
    private void ReadbackComplete(AsyncGPUReadbackRequest request) {
      if (request.hasError) {
        Vlly.Log("GPU readback error detected.");
        return;
      }

      var managedData = request.GetData<byte>().ToArray();
      Task.Run(() => {
        var pngScreenshot = ImageConversion.EncodeArrayToPNG(managedData, GraphicsFormat.R8G8B8_SRGB, (uint)_camera.pixelWidth, (uint)_camera.pixelHeight);
        VllyStorage.EnqueueFrame(_triggerKey, _triggerId, pngScreenshot);
      });
    }

    private bool ShouldCapture() {
      return RealTimeFrameId != _lastFrameId;
    }

    private bool Capture(RenderTexture source, RenderTexture dest) {
      bool doCapture = ShouldCapture();
      _lastFrameId = RealTimeFrameId;
      if (!doCapture) {
        return false;
      }
      _frameCount++;

      AsyncGPUReadback.Request(source, 0, TextureFormat.RGB24, ReadbackComplete);
      Graphics.Blit(source, dest);
      return true;
    }

    public void OnRenderImage(RenderTexture source, RenderTexture dest) {
      if (!_isCapturing || !Capture(source, dest)) {
        // For whatever reason, Capture decided not to capture, so blit.
        Graphics.Blit(source, dest);
      }

    }
    private void OnEndFrameRendering(ScriptableRenderContext context, Camera[] cameras) {
      bool doCapture = ShouldCapture();
      _lastFrameId = RealTimeFrameId;
      if (!doCapture) {
        return;
      }
      _frameCount++;
      AsyncGPUReadback.Request(_renderTexture, 0, TextureFormat.RGB24, ReadbackComplete);
    }
    #endregion
  }
}