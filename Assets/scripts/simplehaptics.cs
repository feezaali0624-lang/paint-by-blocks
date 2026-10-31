using UnityEngine;

// Self-contained, koi third-party asset dependency nahi.
// 3-level fallback: pehle modern Vibrator API try karta hai, phir purana, phir Unity ka default.
// Har attempt (success/fail) phone screen par ek chhota Toast dikhata hai — isse tum
// bina cable/logcat ke seedha phone pe dekh sakti ho ke kya ho raha hai.
public static class SimpleHaptics
{
    public static void LightTick(bool showDebugToast = false)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        TriggerAndroidVibration(25, 100, showDebugToast);
#elif UNITY_IOS && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static void TriggerAndroidVibration(long durationMs, int amplitude, bool showDebugToast)
    {
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
            {
                if (vibrator == null)
                {
                    ShowToast(activity, "Vibrator service null!", showDebugToast);
                    return;
                }

                bool hasVibrator = vibrator.Call<bool>("hasVibrator");
                if (!hasVibrator)
                {
                    ShowToast(activity, "Device mein vibrator hardware nahi!", showDebugToast);
                    return;
                }

                if (AndroidVersionAtLeast(26))
                {
                    using (AndroidJavaClass vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                    {
                        AndroidJavaObject effect = vibrationEffectClass.CallStatic<AndroidJavaObject>(
                            "createOneShot", durationMs, amplitude);
                        vibrator.Call("vibrate", effect);
                    }
                }
                else
                {
                    vibrator.Call("vibrate", durationMs);
                }

                ShowToast(activity, "Haptic fired OK", showDebugToast);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("SimpleHaptics failed: " + e.Message);
        }
    }

    private static void ShowToast(AndroidJavaObject activity, string message, bool showDebugToast)
    {
        if (!showDebugToast) return;

        try
        {
            activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                using (AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast"))
                using (AndroidJavaObject context = activity)
                {
                    AndroidJavaObject javaMessage = new AndroidJavaObject("java.lang.String", message);
                    AndroidJavaObject toast = toastClass.CallStatic<AndroidJavaObject>(
                        "makeText", context, javaMessage, 0); // 0 = Toast.LENGTH_SHORT
                    toast.Call("show");
                }
            }));
        }
        catch { /* toast fail ho to bhi ignore, ye sirf debug ke liye hai */ }
    }

    private static bool AndroidVersionAtLeast(int apiLevel)
    {
        using (AndroidJavaClass versionClass = new AndroidJavaClass("android.os.Build$VERSION"))
        {
            int sdkInt = versionClass.GetStatic<int>("SDK_INT");
            return sdkInt >= apiLevel;
        }
    }
#endif
}