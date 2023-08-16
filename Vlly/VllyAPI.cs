namespace vlly {

  /// <summary>
  /// Core class for interacting with %Vlly.
  /// </summary>
  /// <description>
  /// <p>Open unity project settings and set the properties in the unity inspector (API Key.)</p>
  /// <p>Once you have the vlly settings setup, you can start recording by using <c>Vlly.StartRecording(string triggerKey)</c>.
  /// </description>
  /// <code>
  ///   // Start a recording session with the trigger key "myTriggerKey"
  ///   Vlly.StartRecording("myTriggerKey");
  /// </code>
  public static partial class Vlly {
    public static string VllyUnityVersion = "0.1.0";
    public static void Init() {
      Controller.Initialize();
    }

    /// <summary>
    /// Checks whether Vlly is initialized or not. If it is not, every API will be no-op.
    /// </summary>
    public static bool IsInitialized() {
      bool initialized = Controller.IsInitialized();
      if (!initialized) {
        Vlly.Log("Vlly is not initialized");
      }
      return initialized;
    }

    /// <summary>
    /// Disables Vlly Component. Useful if you have "Manual Initialization" enabled under your Project Settings.
    /// </summary
    public static void Disable() {
        if (!IsInitialized()) return;
        Controller.Disable();
    }

    /// <summary>
    /// Sets the distinct ID of the current user.
    /// </summary>
    /// <param name="uniqueId">a string uniquely identifying this user. Recordings sent to %Vlly
    /// using the same distinct_id will be considered associated with the same user for
    /// reporting and integrations with other systems, so be sure that the given value is globally unique for each
    /// individual user.
    /// </param>
    public static void Identify(string uniqueId) {
      if (!IsInitialized()) return;
      if (VllyStorage.DistinctId == uniqueId) {
        return;
      }
      //TODO: track switch on server
      VllyStorage.DistinctId = uniqueId;
      VllyStorage.HasIdentified = true;
    }

    public static string DistinctId {
      get => VllyStorage.DistinctId;
    }

    public static void stopRecording(string triggerKey = null) {

    }
    public static void StartRecording(string triggerKey, bool stopExisting=false) {
      if (stopExisting) {
        stopRecording();
      }
      Controller.DoStartRecording(triggerKey);
    }


    


  }
}