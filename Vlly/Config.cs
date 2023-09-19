namespace vlly {
  internal static class Config {
    // Can be overriden by VllySettings
    internal static string APIHostAddress = "https://us-central1-vlly-unity.cloudfunctions.net/";
    internal static string APIKey = "";
    internal static bool ShowDebug = false;
    internal static bool ManualInitialization = false;

    internal static int FlushInterval = 3;
    internal static int FramesPerBatch = 20;
    internal static int RecordingFPS = 24;
  }
} 