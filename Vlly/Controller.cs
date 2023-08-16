using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Unity.Collections;
using UnityEngine.Networking;
using UnityEngine.Rendering;

namespace vlly {
  public class Controller : MonoBehaviour {
    private const int _maxFrameCount = 24 * 5; // 24 FPS for 5 Seconds
    private static int _frameCount = 0;
    private const float _targetFPS = 24f; 
    private static float _timeBetweenFrames;
    private static float _frameTimer = 0f;
    private static Camera _mainCamera;
    private static bool _isRecording = false;
    private static string _activeTriggerKey;
    private static string _sessionId;
    private static bool _sentCreateClipEvent = false;

    #region Singleton

    private static Controller _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeBeforeSceneLoad() {
      VllySettings.LoadSettings();
      if (Config.ManualInitialization) {
        return;
      }
      Initialize();
    }

    internal static void Initialize() {
      VllySettings.Instance.ApplyToConfig();
      GetInstance();
      _mainCamera = Camera.main;
      _timeBetweenFrames = 1f / _targetFPS;
    }

    internal static bool IsInitialized() {
      return _instance != null;
    }

    internal static void Disable() {
      if (_instance != null) {
        Destroy(_instance);
      }
    }

    internal static Controller GetInstance () {
      if (_instance == null) {
        GameObject g = new GameObject("Vlly");
        _instance = g.AddComponent<Controller>();
        DontDestroyOnLoad(g);
      }
      return _instance;
    }

    #endregion

    #region API
    internal static void DoStopRecording() {
      _isRecording = false;
      _sentCreateClipEvent = false;
    }
    internal static void DoStartRecording(string triggerKey) {
      VllyStorage.HasRecorded = true;
      if (_isRecording) {
        return;
      }
      _activeTriggerKey = triggerKey;
      _isRecording = true;
      _frameTimer = 0f;
      _frameCount =0;

    }

    #endregion

    #region LifeCycle

    public void Start() {
      VllyPresent();
      CheckForVllyImplemented();
      Vlly.Log("Vlly Component Started");

    }
    private string GetSessionId() {
      if (string.IsNullOrEmpty(_sessionId)) {
        _sessionId = Guid.NewGuid().ToString();
      }
      return _sessionId;
    }
    public void VllyPresent() {
      if (!VllyStorage.HasIntegrated) {
        StartCoroutine(SendHttpEvent("setOnboardingState","HasIntegrated"));
        VllyStorage.HasIntegrated = true;
      }
    } 
    
    private void CheckForVllyImplemented() {
      if (VllyStorage.HasImplemented) {
        return;
      }

      int implementedScore = 0;
      implementedScore += VllyStorage.HasRecorded? 1 : 0;
      implementedScore += VllyStorage.HasIdentified? 1 : 0;

      if (implementedScore >= 1) {
        VllyStorage.HasImplemented = true;
        StartCoroutine(SendHttpEvent("setOnboardingState","HasImplemented"));
      }
    }
    public void Update() {
      if (!IsInitialized()) {
        return;
      }
      if (!_isRecording) {
        return;
      }
      if (_frameCount > _maxFrameCount) {
        DoStopRecording();
        return;
      }
      if (!_sentCreateClipEvent) {
        StartCoroutine(sendCreateClipEvent());
        _sentCreateClipEvent = true;
      }

      _frameTimer += Time.deltaTime;
      if (_frameTimer >= _timeBetweenFrames) {
        StartCoroutine(GetAndSendFrame());
        _frameTimer -= _timeBetweenFrames;
      }
    }


    private IEnumerator GetAndSendFrame() {
      yield return new WaitForEndOfFrame();
      _frameCount ++;
      var tempRT = RenderTexture.GetTemporary(Screen.width, Screen.height, 24);
      ScreenCapture.CaptureScreenshotIntoRenderTexture(tempRT);
      AsyncGPUReadback.Request(tempRT, 0, TextureFormat.RGBA32, ReadbackComplete);
      RenderTexture.ReleaseTemporary(tempRT);
    }

    private void FlipImage(NativeArray<byte> src, NativeArray<byte> target) {
      int width = Screen.width;
      int height = Screen.height;
      for (int i = 0; i < src.Length; i += 4) {
        var arrayIndex = i / 4;
        var x = arrayIndex % width;
        var y = arrayIndex / width;
        var flippedY = (height - 1 - y);
        var flippedIndex = x + flippedY * width;
        target[i] = src[flippedIndex * 4];
        target[i + 1] = src[flippedIndex * 4 + 1];
        target[i + 2] = src[flippedIndex * 4 + 2];
        target[i + 3] = src[flippedIndex * 4 + 3];
      }
    }

    private void ReadbackComplete(AsyncGPUReadbackRequest request) {
      if (request.hasError) {
        Vlly.Log("GPU readback error detected.");
        return;
      }

      var rawData = request.GetData<byte>();
      var writeTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);
      // RenderTexture coordinates are different between OpenGL and DirectX.
      // See more: https://docs.unity3d.com/Manual/SL-PlatformDifferences.html
      GraphicsDeviceType graphicsDevice = SystemInfo.graphicsDeviceType;
      var flipY = graphicsDevice == GraphicsDeviceType.OpenGLCore || 
        graphicsDevice == GraphicsDeviceType.OpenGLES2 ||
        graphicsDevice == GraphicsDeviceType.OpenGLES3 ||
        graphicsDevice == GraphicsDeviceType.Vulkan ?
        false : true;
      
      if (flipY) {
        FlipImage(rawData, writeTexture.GetRawTextureData<byte>());
      } else {
        writeTexture.LoadRawTextureData(rawData);
      }

      var pngScreenshot = ImageConversion.EncodeToPNG(writeTexture);
      List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
      formData.Add(new MultipartFormDataSection("apiKey", VllySettings.Instance.APIKey));
      formData.Add(new MultipartFormDataSection("triggerKey", _activeTriggerKey));
      formData.Add(new MultipartFormDataSection("userId", VllyStorage.DistinctId));
      formData.Add(new MultipartFormDataSection("sessionId", GetSessionId()));
      formData.Add(new MultipartFormFileSection("file", pngScreenshot, "test.png", "image/png"));
      StartCoroutine(SendFrame(formData));

    }
    private IEnumerator SendFrame(List<IMultipartFormSection> formData) {
      using (UnityWebRequest www = UnityWebRequest.Post(VllySettings.Instance.APIHostAddress+"frameIngestion", formData)) {
        yield return www.SendWebRequest();
        if (www.result != UnityWebRequest.Result.Success) {
            Debug.Log(www.error);
        } 
      }
    }
    private IEnumerator sendCreateClipEvent() {
      string jsonString = "{\"triggerKey\": \""+_activeTriggerKey+"\", \"userId\": \""+VllyStorage.DistinctId+"\", \"sessionId\": \""+GetSessionId()+"\"}";
      return DoSendHttpEvent("createClip", jsonString);
    }

    private IEnumerator SendHttpEvent(string eventName, string eventValue) {
      return DoSendHttpEvent(eventName, "\""+eventValue+"\"");
    }

    private IEnumerator DoSendHttpEvent(string eventName, string value) {
      string body = "{\"event\": \""+eventName+"\", \"value\": "+value+","
        + " \"version\": \""+Vlly.VllyUnityVersion+"\", \"apiKey\":\""+VllySettings.Instance.APIKey+"\"}";
      using (UnityWebRequest www = UnityWebRequest.Post(VllySettings.Instance.APIHostAddress+"eventIngestion", body, "application/json")) {
        yield return www.SendWebRequest();
        if (www.result != UnityWebRequest.Result.Success) {
            Vlly.Log(www.error);
        }
      }
    }

    #endregion
  }
}


