namespace vlly {
  internal static class Config {
    // Can be overriden by VllySettings
    internal static string APIHostAddress = "";
    internal static string APIKey = "";
    internal static bool ShowDebug = false;
    internal static bool ManualInitialization = false;

    internal static float FlushInterval = 5f;
    internal static int FramesPerBatch = 12;
    internal static float RecordingFPS = 24f;
  }
} 