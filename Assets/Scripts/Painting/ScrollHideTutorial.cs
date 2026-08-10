using UnityEngine;
using UnityEngine.UI;

// Attach this to the color ScrollRect GameObject.
// Hides the tutorial hand the moment the player scrolls.
[RequireComponent(typeof(ScrollRect))]
public class ScrollHideTutorial : MonoBehaviour
{
    void Start()
    {
        GetComponent<ScrollRect>().onValueChanged.AddListener(OnScrolled);
    }

    void OnScrolled(Vector2 _)
    {
        PaintingTutorialManager.Instance?.HideTutorial();
    }
}
