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
using UnityEngine.Experimental.Rendering;


namespace vlly {
  public class Controller : MonoBehaviour {
    private static  int _maxFrameCount;
    private static int _frameCount = 0;
    private static float _timeBetweenFrames;
    private static float _frameTimer = 0f;
    private static bool _isRecording = false;
    private static string _activeTriggerKey;
    private static string _activeTriggerId;
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
      _timeBetweenFrames = 1f / Config.RecordingFPS;
      // TODO: Pull from the trigger settings after BETA.
      _maxFrameCount = (int) (Config.RecordingFPS * 5); // 24 FPS for 5 Seconds
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
      _activeTriggerId = GetTriggerId();
      _isRecording = true;
      _frameTimer = 0f;
      _frameCount = 0;

    }

    #endregion

    #region LifeCycle

    public void Start() {
      VllyPresent();
      CheckForVllyImplemented();
      Vlly.Log("Vlly Component Started");
      StartCoroutine(WaitAndFlush());

    }

    private string GetSessionId() {
      if (string.IsNullOrEmpty(_sessionId)) {
        _sessionId = Guid.NewGuid().ToString();
      }
      return _sessionId;
    }

    private static string GetTriggerId() {
      return Guid.NewGuid().ToString();
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

    private IEnumerator WaitAndFlush() {
      while (true) {
        yield return new WaitForSecondsRealtime(Config.FlushInterval);
        if (!_isRecording) {
          StartCoroutine(SendFrames());
        }
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
      var tempRT = RenderTexture.GetTemporary(Camera.main.pixelWidth, Camera.main.pixelHeight, 0);
      ScreenCapture.CaptureScreenshotIntoRenderTexture(tempRT);
      AsyncGPUReadback.Request(tempRT, 0, TextureFormat.RGB24, ReadbackComplete);
      RenderTexture.ReleaseTemporary(tempRT);
    }
    private void ReadbackComplete(AsyncGPUReadbackRequest request) {
      if (request.hasError) {
        Vlly.Log("GPU readback error detected.");
        return;
      }

      var rawData = request.GetData<byte>();
      var pngScreenshot = ImageConversion.EncodeNativeArrayToPNG(rawData, GraphicsFormat.R8G8B8_SRGB, (uint)Camera.main.pixelWidth, (uint)Camera.main.pixelHeight);
      VllyStorage.EnqueueFrame(_activeTriggerKey, _activeTriggerId, pngScreenshot.ToArray());
    }
    private IEnumerator SendFrames() {
      var batchData = VllyStorage.DequeueBatchFrames(Config.FramesPerBatch);
      if (batchData.triggerKey == null) {
        yield break;
      }
      Vlly.Log("Send Frames");
      Vlly.Log(batchData.frames.Count.ToString());

      List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
      formData.Add(new MultipartFormDataSection("apiKey", VllySettings.Instance.APIKey));
      formData.Add(new MultipartFormDataSection("userId", VllyStorage.DistinctId));
      formData.Add(new MultipartFormDataSection("sessionId", GetSessionId()));
      formData.Add(new MultipartFormDataSection("triggerKey", batchData.triggerKey));
      formData.Add(new MultipartFormDataSection("triggerId", batchData.triggerId));
      batchData.frames.ForEach(((double timestamp, byte[] data) frameData) => {
        formData.Add(new MultipartFormFileSection(frameData.timestamp.ToString(), frameData.data, frameData.timestamp.ToString()+".png", "image/png"));
      });
      using (UnityWebRequest www = UnityWebRequest.Post(VllySettings.Instance.APIHostAddress+"frameIngestion", formData)) {
        yield return www.SendWebRequest();
        #if UNITY_2020_1_OR_NEWER
        if (www.result != UnityWebRequest.Result.Success)
        #else
        if (www.isHttpError || www.isNetworkError)
        #endif
        {
          Vlly.Log(www.error);
        } else {
          VllyStorage.DeleteBatchFrames(batchData.triggerKey, batchData.triggerId, Config.FramesPerBatch);
        }
      }
    } 

    private IEnumerator SendFrame(List<IMultipartFormSection> formData) {
      using (UnityWebRequest www = UnityWebRequest.Post(VllySettings.Instance.APIHostAddress+"frameIngestion", formData)) {
        yield return www.SendWebRequest();
        if (www.result != UnityWebRequest.Result.Success) {
            Vlly.Log(www.error);
        } 
      }
    }
    private IEnumerator sendCreateClipEvent() {
      string jsonString = "{\"triggerKey\": \""+_activeTriggerKey+"\", \"userId\": \""+VllyStorage.DistinctId+"\", \"sessionId\": \""+GetSessionId()+"\", \"triggerId\": \""+_activeTriggerId+"\"}";
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


