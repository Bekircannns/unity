using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class BootLoader : MonoBehaviour
{
    [SerializeField] private float bootDelaySeconds = 0.25f;

    private IEnumerator Start()
    {
        // Mobile baseline: enforce portrait early in startup flow.
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
        Screen.orientation = ScreenOrientation.Portrait;
        Application.targetFrameRate = 60;

        GameRunState.LoadProgress();

        yield return new WaitForSeconds(bootDelaySeconds);
        SceneManager.LoadScene(SceneNames.Menu);
    }
}
