using System;
using System.Collections.Generic;

namespace vlly {
  public static class VllyStorage {

    // triggerKey: { triggerId: [(time1, frameData)] }
    private static Dictionary<string, Dictionary<string, List<(double timestamp, byte[] data)>>>  frameStorage = new Dictionary<string, Dictionary<string, List<(double, byte[])>>>();

    #region Preferences
    private static IPreferences PreferencesSource = new PlayerPreferences();

    public static void SetPreferencesSource(IPreferences preferences) {
      PreferencesSource = preferences;
    }

    #endregion

    #region HasIntegrated

    private const string HasIntegratedName = "Vlly.HasIntegrated";

    internal static bool HasIntegrated {
      get => Convert.ToBoolean(PreferencesSource.GetInt(HasIntegratedName, 0));
      set => PreferencesSource.SetInt(HasIntegratedName, Convert.ToInt32(value));
    }

    #endregion

    #region HasImplemented

    private const string HasImplementedName = "Vlly.HasImplemented";

    internal static bool HasImplemented {
      get => Convert.ToBoolean(PreferencesSource.GetInt(HasImplementedName, 0));
      set => PreferencesSource.SetInt(HasImplementedName, Convert.ToInt32(value));
    }

    #endregion

    #region HasIdentified

    private const string HasIdentifiedName = "Vlly.HasIdentified";

    internal static bool HasIdentified {
      get => Convert.ToBoolean(PreferencesSource.GetInt(HasIdentifiedName, 0));
      set => PreferencesSource.SetInt(HasIdentifiedName, Convert.ToInt32(value));
    }

    #endregion

    #region HasRecorded

    private const string HasRecordedName = "Vlly.HasRecorded";

    public static bool HasRecorded {
      get => Convert.ToBoolean(PreferencesSource.GetInt(HasRecordedName, 0));
      set => PreferencesSource.SetInt(HasRecordedName, Convert.ToInt32(value));
    }

    #endregion

    #region DistinctId
    
    private const string DistinctIdName = "Vlly.DistinctId";
    
    private static string _distinctId;
    
    public static string DistinctId {
      get {
        if (!string.IsNullOrEmpty(_distinctId))  {
          return _distinctId;
        }
        if (PreferencesSource.HasKey(DistinctIdName)) {
            _distinctId = PreferencesSource.GetString(DistinctIdName);
        }
        // Generate a Unique ID for this client if still null or empty
        // https://devblogs.microsoft.com/oldnewthing/?p=21823
        if (string.IsNullOrEmpty(_distinctId)) {
          DistinctId = Guid.NewGuid().ToString();
        }
        return _distinctId;
      }
      set {
          _distinctId = value;
          PreferencesSource.SetString(DistinctIdName, _distinctId);
      }
    }
    
    #endregion

    #region Track
    internal static void EnqueueFrame(string triggerKey, string triggerId, byte[] framePNG) {
      if (!frameStorage.ContainsKey(triggerKey)) {
        frameStorage[triggerKey] = new Dictionary<string, List<(double, byte[])>>() {
          [triggerId] = new List<(double timestamp, byte[] data)>()
        };
      }
      frameStorage[triggerKey][triggerId].Add((Util.CurrentTimeInMilliseconds(), framePNG));
    }

    internal static (string triggerKey, string triggerId, List<(double, byte[])> frames) DequeueBatchFrames (int batchSize) {
      var triggerKeyEnumerator = frameStorage.GetEnumerator(); 
      if(!triggerKeyEnumerator.MoveNext()) {
        // Nothing to send
        return (null, null, null);
      };
      string triggerKey = triggerKeyEnumerator.Current.Key;
      var triggerIdToList = triggerKeyEnumerator.Current.Value;
      var triggerIdEnumerator = triggerIdToList.GetEnumerator();
      if (!triggerIdEnumerator.MoveNext()) {
        frameStorage.Remove(triggerKey);
        // Nothing to send
        return (null, null, null);

      }
      string triggerId = triggerIdEnumerator.Current.Key;
      var remainingFrameData = triggerIdEnumerator.Current.Value;
      int remainingFramesCount = remainingFrameData.Count;
      var batchList = remainingFrameData.GetRange(0, Math.Min(batchSize, remainingFramesCount));
      return (triggerKey, triggerId, batchList);
    }

    // Remove sent frames from storage after successfully sending them. 
    internal static void DeleteBatchFrames (string triggerKey, string triggerId, int batchSize) {
      int remainingFramesCount = frameStorage[triggerKey][triggerId].Count;
      frameStorage[triggerKey][triggerId].RemoveRange(0, Math.Min(batchSize, remainingFramesCount));
      if (frameStorage[triggerKey][triggerId].Count == 0) {
        frameStorage[triggerKey].Remove(triggerId);
      }
      if (frameStorage[triggerKey].Count == 0) {
        frameStorage.Remove(triggerKey);
      }
    }

    #endregion
  }
}