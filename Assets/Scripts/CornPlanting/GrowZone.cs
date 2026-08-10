using UnityEngine;

public class GrowZone : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Cow's RectTransform (must have CowController on same GameObject)")]
    public RectTransform cow;

    [Header("Settings")]
    [Tooltip("Allow the zone to fire again after a reset")]
    public bool repeatable = false;

    private RectTransform _zone;
    private CowController _cowController;
    private bool _triggered = false;
    private bool _cowWasInside = false;

    void Awake()
    {
        _zone = GetComponent<RectTransform>();
        _cowController = cow != null ? cow.GetComponent<CowController>() : null;
    }

    void Update()
    {
        if (cow == null) return;
        if (_triggered && !repeatable) return;

        bool isInside = OverlapRotated();

        if (isInside && !_cowWasInside)
        {
            _cowWasInside = true;

            if (!OpenFieldGameManager.Instance.IsGameStarted)   // ← fixed
            {
                _triggered = true;

                if (_cowController != null)
                    _cowController.ForceIdle();

                OpenFieldGameManager.Instance.ShowGrowPopup();   // ← fixed
            }
        }

        if (!isInside)
            _cowWasInside = false;
    }

    bool OverlapRotated()
    {
        Rect zoneLocalRect = _zone.rect;
        Rect cowLocalRect = cow.rect;

        Vector3[] cowCorners = new Vector3[4];
        cow.GetWorldCorners(cowCorners);

        foreach (Vector3 wc in cowCorners)
        {
            Vector2 lp = _zone.InverseTransformPoint(wc);
            if (zoneLocalRect.Contains(lp, true))
                return true;
        }

        Vector3[] zoneCorners = new Vector3[4];
        _zone.GetWorldCorners(zoneCorners);

        foreach (Vector3 wc in zoneCorners)
        {
            Vector2 lp = cow.InverseTransformPoint(wc);
            if (cowLocalRect.Contains(lp, true))
                return true;
        }

        return false;
    }

    public void ResetZone()
    {
        _triggered = false;
        _cowWasInside = false;
    }

    void OnDrawGizmosSelected()
    {
        if (_zone == null) _zone = GetComponent<RectTransform>();
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.8f);
        Vector3[] c = new Vector3[4];
        _zone.GetWorldCorners(c);
        for (int i = 0; i < 4; i++)
            Gizmos.DrawLine(c[i], c[(i + 1) % 4]);
    }
}