using UnityEngine;

public class RandomButtonVisibility : MonoBehaviour
{
    [Header("Buttons")]
    public GameObject button1;
    public GameObject button2;
    public GameObject button3;

    // The food button that was selected this round — read by RoundManager for bubble
    public GameObject SelectedButton { get; private set; }

    void Start()
    {
        ShowRandomButton();
    }

    public void ShowRandomButton()
    {
        if (button1 != null) button1.SetActive(false);
        if (button2 != null) button2.SetActive(false);
        if (button3 != null) button3.SetActive(false);

        // Build a list of only the assigned buttons so we never pick a null one
        var available = new System.Collections.Generic.List<GameObject>();
        if (button1 != null) available.Add(button1);
        if (button2 != null) available.Add(button2);
        if (button3 != null) available.Add(button3);

        if (available.Count == 0)
        {
            Debug.LogWarning("RandomButtonVisibility → No buttons assigned!");
            SelectedButton = null;
            return;
        }

        int randomIndex = Random.Range(0, available.Count);
        SelectedButton = available[randomIndex];
        SelectedButton.SetActive(true);
    }
}