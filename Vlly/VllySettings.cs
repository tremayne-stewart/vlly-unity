using System;
using System.IO;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace vlly {
  public class VllySettings : ScriptableObject {
    [Tooltip("If true will print helpful debugging messages")]
    public bool ShowDebug;
    [Tooltip("If true, you need to manually initialize the library")]
    public bool ManualInitialization;
    [Tooltip("The api host of where to send the requests to. Useful when you need to proxy all the request to somewhere else.'")]
    public string APIHostAddress = "https://us-central1-vlly-unity.cloudfunctions.net/";
    [Tooltip("Your Vlly API Key. You can find it in your Vlly dashboard.")]
    public string APIKey = "";


    public void ApplyToConfig () {
      Config.ShowDebug = ShowDebug;
      Config.ManualInitialization = ManualInitialization;
      Config.APIKey = APIKey;
      string host = APIHostAddress.EndsWith("/") ? APIHostAddress : $"{APIHostAddress}/";
    }

    #region static
    private static VllySettings _instance;

    public static void LoadSettings() {
      if (!_instance) {
        _instance = FindOrCreateInstance();
        _instance.ApplyToConfig();
      }
    }

    public static VllySettings Instance {
      get {
        LoadSettings();
        return _instance;
      }
    }

    private static VllySettings FindOrCreateInstance() {
      VllySettings instance = null;
      instance = instance ? null : Resources.Load<VllySettings>("Vlly");
      instance = instance ? instance : Resources.LoadAll<VllySettings>(string.Empty).FirstOrDefault();
      instance = instance ? instance : CreateAndSave<VllySettings>();
      if (instance == null) throw new Exception("Could not find or create settings for Vlly");
      return instance;
    }

    private static T CreateAndSave<T>() where T : ScriptableObject {
      T instance = CreateInstance<T>();
#if UNITY_EDITOR
      //Saving during Awake() will crash Unity, delay saving until next editor frame
      if (EditorApplication.isPlayingOrWillChangePlaymode) {
          EditorApplication.delayCall += () => SaveAsset(instance);
      }
      else {
          SaveAsset(instance);
      }
#endif
      return instance;
    }

#if UNITY_EDITOR
    private static void SaveAsset<T>(T obj) where T : ScriptableObject {

        string dirName = "Assets/Resources";
        if (!Directory.Exists(dirName))
        {
            Directory.CreateDirectory(dirName);
        }
        AssetDatabase.CreateAsset(obj, "Assets/Resources/Vlly.asset");
        AssetDatabase.SaveAssets();
    }
#endif
    #endregion

  }
}