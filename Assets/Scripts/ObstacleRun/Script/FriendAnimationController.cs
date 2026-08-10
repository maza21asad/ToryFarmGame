using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FriendAnimationController : MonoBehaviour
{
    [Header("Celebrate Animation")]
    public Sprite[] celebrateFrames;
    public float celebrateFPS = 12f;

    private Image friendImage;
    private bool isPlaying = false;

    void Start()
    {
        friendImage = GetComponent<Image>();
        //gameObject.SetActive(false);
    }

    // =========================
    public void PlayCelebrate()
    {
        if (isPlaying) return;

        gameObject.SetActive(true); // show friend
        isPlaying = true;
        StartCoroutine(Animate());
    }

    IEnumerator Animate()
    {
        int i = 0;

        while (true)
        {
            if (celebrateFrames == null || celebrateFrames.Length == 0)
                yield break;

            friendImage.sprite = celebrateFrames[i];
            i = (i + 1) % celebrateFrames.Length;

            yield return new WaitForSeconds(1f / celebrateFPS);
        }
    }
}