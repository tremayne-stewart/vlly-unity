using System;
namespace vlly {
  public static class VllyStorage {

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

    #region SessionId

    private const string SessionIdName = "Vlly.SessionId";

    private static string _sessionId;

    public static string SessionId {
      get {
        if (!string.IsNullOrEmpty(_sessionId))  {
          return _sessionId;
        }
        if (PreferencesSource.HasKey(SessionIdName)) {
            _sessionId = PreferencesSource.GetString(SessionIdName);
        }
        // Generate a Unique ID for this client if still null or empty
        // https://devblogs.microsoft.com/oldnewthing/?p=21823
        if (string.IsNullOrEmpty(_sessionId)) {
          SessionId = Guid.NewGuid().ToString();
        }
        return _sessionId;
      }
      set {
        _sessionId = value;
        PreferencesSource.SetString(SessionIdName, _sessionId);

      }
    }
    #endregion
  }
}