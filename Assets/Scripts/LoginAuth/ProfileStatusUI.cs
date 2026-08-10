//////using UnityEngine;
//////using TMPro;

//////public class ProfileStatusUI : MonoBehaviour
//////{
//////    [Header("Profile State")]
//////    public GameObject profileLogoObject;   // shown when no one is logged in
//////    public GameObject profileLoginObject;  // shown once a profile is logged in
//////    public TMP_Text userNameText;          // shows the logged-in user's name

//////    void OnEnable()
//////    {
//////        Refresh();
//////        if (PlayFabManager.Instance != null)
//////            PlayFabManager.Instance.OnLoginSuccess += Refresh;
//////    }

//////    void OnDisable()
//////    {
//////        if (PlayFabManager.Instance != null)
//////            PlayFabManager.Instance.OnLoginSuccess -= Refresh;
//////    }

//////    public void Refresh()
//////    {
//////        bool loggedIn = PlayFabManager.Instance != null && PlayFabManager.Instance.IsLoggedIn;

//////        if (profileLogoObject != null) profileLogoObject.SetActive(!loggedIn);
//////        if (profileLoginObject != null) profileLoginObject.SetActive(loggedIn);

//////        if (userNameText != null)
//////            userNameText.text = loggedIn ? PlayFabManager.Instance.DisplayName : "";
//////    }
//////}

////using UnityEngine;
////using TMPro;

////public class ProfileStatusUI : MonoBehaviour
////{
////    [Header("Profile State")]
////    public GameObject profileLogoObject;   // shown when no one is logged in
////    public GameObject profileLoginObject;  // shown once a profile is logged in
////    public TMP_Text userNameText;          // shows the logged-in user's name

////    void OnEnable()
////    {
////        Refresh();
////        if (PlayFabManager.Instance != null)
////        {
////            PlayFabManager.Instance.OnLoginSuccess += Refresh;
////            PlayFabManager.Instance.OnLogout += Refresh;
////        }
////    }

////    void OnDisable()
////    {
////        if (PlayFabManager.Instance != null)
////        {
////            PlayFabManager.Instance.OnLoginSuccess -= Refresh;
////            PlayFabManager.Instance.OnLogout -= Refresh;
////        }
////    }

////    public void Refresh()
////    {
////        bool loggedIn = PlayFabManager.Instance != null && PlayFabManager.Instance.IsLoggedIn;

////        if (profileLogoObject != null) profileLogoObject.SetActive(!loggedIn);
////        if (profileLoginObject != null) profileLoginObject.SetActive(loggedIn);

////        if (userNameText != null)
////            userNameText.text = loggedIn ? PlayFabManager.Instance.DisplayName : "";
////    }
////}

//using UnityEngine;
//using TMPro;

//public class ProfileStatusUI : MonoBehaviour
//{
//    [Header("Profile State")]
//    public GameObject profileLogoObject;   // shown when no one is logged in
//    public GameObject profileLoginObject;  // shown once a profile is logged in
//    public TMP_Text userNameText;          // shows the logged-in user's name

//    void OnEnable()
//    {
//        Refresh();
//        if (PlayFabManager.Instance != null)
//        {
//            PlayFabManager.Instance.OnLoginSuccess += Refresh;
//            PlayFabManager.Instance.OnLogout += Refresh;
//        }
//    }

//    void OnDisable()
//    {
//        if (PlayFabManager.Instance != null)
//        {
//            PlayFabManager.Instance.OnLoginSuccess -= Refresh;
//            PlayFabManager.Instance.OnLogout -= Refresh;
//        }
//    }

//    public void Refresh()
//    {
//        bool loggedIn = PlayFabManager.Instance != null && PlayFabManager.Instance.IsLoggedIn;

//        if (profileLogoObject != null) profileLogoObject.SetActive(!loggedIn);
//        if (profileLoginObject != null) profileLoginObject.SetActive(loggedIn);

//        if (userNameText != null)
//            userNameText.text = loggedIn ? PlayFabManager.Instance.DisplayName : "No user";
//    }
//}


using UnityEngine;
using TMPro;

public class ProfileStatusUI : MonoBehaviour
{
    [Header("Profile State")]
    public GameObject profileLogoObject;   // shown when no one is logged in
    public GameObject profileLoginObject;  // shown once a profile is logged in
    public TMP_Text userNameText;          // shows the logged-in user's name

    void OnEnable()
    {
        Refresh();
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.OnLoginSuccess += Refresh;
            PlayFabManager.Instance.OnLogout += Refresh;
        }
    }

    void OnDisable()
    {
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.OnLoginSuccess -= Refresh;
            PlayFabManager.Instance.OnLogout -= Refresh;
        }
    }

    public void Refresh()
    {
        bool loggedIn = PlayFabManager.Instance != null && PlayFabManager.Instance.IsLoggedIn;

        if (profileLogoObject != null) profileLogoObject.SetActive(!loggedIn);
        if (profileLoginObject != null) profileLoginObject.SetActive(loggedIn);

        if (userNameText != null)
            userNameText.text = loggedIn ? PlayFabManager.Instance.DisplayName : "No user";
    }
}