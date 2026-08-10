//////using UnityEngine;

//////public class OpenLoginButton : MonoBehaviour
//////{
//////    public GameObject loginPanel;

//////    public void OnClick()
//////    {
//////        if (PlayFabManager.Instance.IsLoggedIn) return; // already logged in, nothing to do

//////        if (MenuManager.instance != null) MenuManager.instance.ShowPanel(loginPanel);
//////        else if (GameManager.Instance != null) GameManager.Instance.ShowPanel(loginPanel);
//////        else if (UIManager.Instance != null) UIManager.Instance.ShowPanel(loginPanel);
//////    }
//////}

////using UnityEngine;

////public class OpenLoginButton : MonoBehaviour
////{
////    public GameObject loginPanel;

////    public void OnClick()
////    {
////        if (MenuManager.instance != null) MenuManager.instance.ShowPanel(loginPanel);
////        else if (GameManager.Instance != null) GameManager.Instance.ShowPanel(loginPanel);
////        else if (UIManager.Instance != null) UIManager.Instance.ShowPanel(loginPanel);
////    }
////}

//using UnityEngine;
//using TMPro;

//public class OpenLoginButton : MonoBehaviour
//{
//    public GameObject loginPanel;
//    public TMP_Text buttonLabel; // shows "Log In" or "Log Out"

//    void OnEnable()
//    {
//        RefreshLabel();
//        if (PlayFabManager.Instance != null)
//        {
//            PlayFabManager.Instance.OnLoginSuccess += RefreshLabel;
//            PlayFabManager.Instance.OnLogout += RefreshLabel;
//        }
//    }

//    void OnDisable()
//    {
//        if (PlayFabManager.Instance != null)
//        {
//            PlayFabManager.Instance.OnLoginSuccess -= RefreshLabel;
//            PlayFabManager.Instance.OnLogout -= RefreshLabel;
//        }
//    }

//    public void OnClick()
//    {
//        if (PlayFabManager.Instance != null && PlayFabManager.Instance.IsLoggedIn)
//        {
//            PlayFabManager.Instance.Logout();
//            RefreshLabel();
//            return;
//        }

//        if (MenuManager.instance != null) MenuManager.instance.ShowPanel(loginPanel);
//        else if (GameManager.Instance != null) GameManager.Instance.ShowPanel(loginPanel);
//        else if (UIManager.Instance != null) UIManager.Instance.ShowPanel(loginPanel);
//    }

//    private void RefreshLabel()
//    {
//        if (buttonLabel == null) return;
//        bool loggedIn = PlayFabManager.Instance != null && PlayFabManager.Instance.IsLoggedIn;
//        buttonLabel.text = loggedIn ? "Log Out" : "Log In";
//    }
//}

using UnityEngine;
using TMPro;

public class OpenLoginButton : MonoBehaviour
{
    public GameObject loginPanel;
    public TMP_Text buttonLabel; // shows "Log In" or "Log Out"

    void OnEnable()
    {
        RefreshLabel();
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.OnLoginSuccess += RefreshLabel;
            PlayFabManager.Instance.OnLogout += RefreshLabel;
        }
    }

    void OnDisable()
    {
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.OnLoginSuccess -= RefreshLabel;
            PlayFabManager.Instance.OnLogout -= RefreshLabel;
        }
    }

    public void OnClick()
    {
        if (PlayFabManager.Instance != null && PlayFabManager.Instance.IsLoggedIn)
        {
            PlayFabManager.Instance.Logout();
            RefreshLabel();
            return;
        }

        if (MenuManager.instance != null) MenuManager.instance.ShowPanel(loginPanel);
        else if (GameManager.Instance != null) GameManager.Instance.ShowPanel(loginPanel);
        else if (UIManager.Instance != null) UIManager.Instance.ShowPanel(loginPanel);
    }

    private void RefreshLabel()
    {
        if (buttonLabel == null) return;
        bool loggedIn = PlayFabManager.Instance != null && PlayFabManager.Instance.IsLoggedIn;
        buttonLabel.text = loggedIn ? "Log Out" : "Log In";
    }
}