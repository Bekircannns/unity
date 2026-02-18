using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneAutoInstaller
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureSceneController()
    {
        var activeScene = SceneManager.GetActiveScene().name;
        switch (activeScene)
        {
            case SceneNames.Boot:
                EnsureComponent<BootLoader>("BootController");
                break;
            case SceneNames.Menu:
                EnsureComponent<MenuController>("MenuController");
                break;
            case SceneNames.Gameplay:
                EnsureComponent<GameplayController>("GameplayController");
                break;
            case SceneNames.Results:
                EnsureComponent<ResultsController>("ResultsController");
                break;
        }
    }

    private static void EnsureComponent<T>(string objectName) where T : Component
    {
        if (Object.FindFirstObjectByType<T>() != null)
        {
            return;
        }

        var host = new GameObject(objectName);
        host.AddComponent<T>();
    }
}
