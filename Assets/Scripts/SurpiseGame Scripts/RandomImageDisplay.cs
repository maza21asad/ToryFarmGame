using UnityEngine;
using UnityEngine.UI;

public class RandomImageDisplay : MonoBehaviour
{
    public Sprite[] images = new Sprite[3];
    public Image displayImage;

    private bool isOpen = false;

    public void ShowRandomImage()
    {
        if (isOpen) return;

        isOpen = true;

        int randomIndex = Random.Range(0, images.Length);
        displayImage.sprite = images[randomIndex];
    }
}
