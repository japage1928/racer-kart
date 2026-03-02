using UnityEngine;

public class BrowserRuntimeSettings : MonoBehaviour
{
    private void Awake()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLInput.captureAllKeyboardInput = true;
#endif
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
    }
}
