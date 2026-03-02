using UnityEngine;

public class RacerRuntimeBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureBootstrap()
    {
        if (FindFirstObjectByType<RacerGameManager>() != null || FindFirstObjectByType<RacerRuntimeBootstrap>() != null)
        {
            return;
        }

        var bootstrapGo = new GameObject("RacerRuntimeBootstrap");
        bootstrapGo.AddComponent<RacerRuntimeBootstrap>();
    }

    private void Awake()
    {
        gameObject.AddComponent<BrowserRuntimeSettings>();
        var builder = gameObject.AddComponent<RacerSceneBuilder>();
        var result = builder.BuildScene();

        var gameManager = gameObject.AddComponent<RacerGameManager>();
        gameManager.Initialize(result);
    }
}
