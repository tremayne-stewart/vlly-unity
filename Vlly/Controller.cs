using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace vlly {
  public class Controller : MonoBehaviour {
    private static  int _maxFrameCount;
    private static bool _isRecording = false;
    private static string _activeTriggerKey;
    private static string _activeTriggerId;
    private static string _sessionId;
    private static bool _sentCreateClipEvent = false;
    private static VllyCamera _vllyCamera;

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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeAfterSceneLoad() {
      InitializeCamera();
    }

    internal static void Initialize() {
      VllySettings.Instance.ApplyToConfig();
      GetInstance();
      _maxFrameCount = (int) (Config.RecordingFPS * 2); // 24 FPS for 5 Seconds
    }

    internal static bool IsInitialized() {
      return _instance != null;
    }

    internal static void Disable() {
      if (_instance != null) {
        Destroy(_instance);
      }
      if (_vllyCamera != null) {
        Destroy(_vllyCamera);
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

    internal static VllyCamera InitializeCamera() {
        GameObject cameraObject = GameObject.Find("/VllyCamera");
        if (cameraObject == null) {
          Vlly.LogError("No VllyCamera found. Please add a Camera with the name 'VllyCamera' to the top level of the Hierarchy.");
          return null;
        }
        _vllyCamera = cameraObject.AddComponent<VllyCamera>();
        return _vllyCamera;
    }

    #endregion

    #region API
    internal static void DoStopRecording() {
      _isRecording = false;
      _sentCreateClipEvent = false;
      _vllyCamera.StopRecording();
    }
    internal static void DoStartRecording(string triggerKey) {
      VllyStorage.HasRecorded = true;
      if (_isRecording) {
        return;
      }

      _activeTriggerKey = triggerKey;
      _activeTriggerId = GetTriggerId();
      _isRecording = true;

      _vllyCamera.StartRecording(_activeTriggerKey, _activeTriggerId);
    }

    #endregion

    #region LifeCycle

    public void Start() {
      VllyPresent();
      CheckForVllyImplemented();
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
      if (_vllyCamera.RecordedFrameCount >= _maxFrameCount) {
        DoStopRecording();
        return;
      }
      if (!_sentCreateClipEvent) {
        StartCoroutine(sendCreateClipEvent());
        _sentCreateClipEvent = true;
      }
    }
    #endregion

    #region Requests
    private IEnumerator SendFrames() {
      var batchData = VllyStorage.DequeueBatchFrames(Config.FramesPerBatch);
      if (batchData.triggerKey == null) {
        yield break;
      }

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
      using (UnityWebRequest www = UnityWebRequest.Put(VllySettings.Instance.APIHostAddress+"eventIngestion", body)) {
        www.SetRequestHeader("Content-Type", "application/json");
        yield return www.SendWebRequest();
        if (www.result != UnityWebRequest.Result.Success) {
            Vlly.Log(www.error);
        }
      }
    }

    #endregion
  }
}


