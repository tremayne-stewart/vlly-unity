using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Http;
using System.Web;

namespace vlly {
    internal static class Util {
        internal static double CurrentTimeInMilliseconds() {
            DateTime epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            long currentEpochTime = (long)(DateTime.UtcNow - epochStart).TotalMilliseconds;
            return currentEpochTime;
        }
    }
}
