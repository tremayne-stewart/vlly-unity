using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System.Diagnostics;

namespace vlly {
  public class VllyCamera : MonoBehaviour {
    private static VllyCamera _instance;
    private static Camera _mainCamera;
    private System.Diagnostics.Stopwatch _frameTimer;
    private bool _isCapturing = false;
    private int _frameCount = 0;
    private int _lastFrameId = 0;
    private string _triggerKey;
    private string _triggerId;
    public int RecordedFrameCount {
      get {
        return _frameCount;
      }
    }
    public int RealTimeFrameId {
      get {
        return (int) (_frameTimer.ElapsedMilliseconds / 1000.0m * Config.RecordingFPS);
      }
    }

    #region Singleton
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeBeforeSceneLoad() {
      // Local cache to avoid slight CPU overhead of Unity cache lookup.
      _mainCamera = Camera.main;
      _instance = _mainCamera.gameObject.AddComponent<VllyCamera>();
    }

    public static VllyCamera GetInstance() {
      return _instance;
    }
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

    #region Capture
    private void ReadbackComplete(AsyncGPUReadbackRequest request) {
      if (request.hasError) {
        Vlly.Log("GPU readback error detected.");
        return;
      }

      var managedData = request.GetData<byte>().ToArray();
      Task.Run(() => {
        var pngScreenshot = ImageConversion.EncodeArrayToPNG(managedData, GraphicsFormat.R8G8B8_SRGB, (uint)_mainCamera.pixelWidth, (uint)_mainCamera.pixelHeight);
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

      Vlly.Log("Should CAp - trigger read from source");
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
    #endregion
  }
}