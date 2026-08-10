////using PlayFab;
////using PlayFab.ClientModels;
////using System;
////using UnityEngine;

////public class PlayFabManager : MonoBehaviour
////{
////    public static PlayFabManager Instance;

////    public bool IsLoggedIn { get; private set; }
////    public string DisplayName { get; private set; }
////    public event Action OnLoginSuccess;
////    public event Action OnLogout;

////    void Awake()
////    {
////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
////        Instance = this;
////        DontDestroyOnLoad(gameObject);
////    }

////    public void Register(string email, string password, string username, Action onSuccess, Action<string> onError)
////    {
////        var request = new RegisterPlayFabUserRequest
////        {
////            Email = email,
////            Password = password,
////            RequireBothUsernameAndEmail = false
////        };

////        PlayFabClientAPI.RegisterPlayFabUser(request,
////            result => SetDisplayName(username, onSuccess, onError),
////            error => onError?.Invoke(error.GenerateErrorReport()));
////    }

////    private void SetDisplayName(string username, Action onSuccess, Action<string> onError)
////    {
////        var request = new UpdateUserTitleDisplayNameRequest { DisplayName = username };

////        PlayFabClientAPI.UpdateUserTitleDisplayName(request,
////            result =>
////            {
////                DisplayName = username;
////                IsLoggedIn = true;
////                OnLoginSuccess?.Invoke();
////                onSuccess?.Invoke();
////            },
////            error => onError?.Invoke("Account created, but username failed: " + error.GenerateErrorReport()));
////    }

////    public void Login(string email, string password, Action onSuccess, Action<string> onError)
////    {
////        var request = new LoginWithEmailAddressRequest
////        {
////            Email = email,
////            Password = password,
////            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
////            {
////                GetPlayerProfile = true,
////                ProfileConstraints = new PlayerProfileViewConstraints { ShowDisplayName = true }
////            }
////        };

////        PlayFabClientAPI.LoginWithEmailAddress(request,
////            result =>
////            {
////                DisplayName = result.InfoResultPayload?.PlayerProfile?.DisplayName;
////                IsLoggedIn = true;
////                OnLoginSuccess?.Invoke();
////                onSuccess?.Invoke();
////            },
////            error => onError?.Invoke(error.GenerateErrorReport()));
////    }

////    public void Logout()
////    {
////        if (!IsLoggedIn) return;

////        PlayFabClientAPI.ForgetAllCredentials();
////        IsLoggedIn = false;
////        DisplayName = null;
////        OnLogout?.Invoke();
////    }
////}

//using PlayFab;
//using PlayFab.ClientModels;
//using System;
//using System.Collections.Generic;
//using UnityEngine;

//public class PlayFabManager : MonoBehaviour
//{
//    public static PlayFabManager Instance;

//    public bool IsLoggedIn { get; private set; }
//    public string DisplayName { get; private set; }
//    public event Action OnLoginSuccess;
//    public event Action OnLogout;

//    void Awake()
//    {
//        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//        Instance = this;
//        DontDestroyOnLoad(gameObject);
//    }

//    public void Register(string email, string password, string username, string phoneNumber, Action onSuccess, Action<string> onError)
//    {
//        var request = new RegisterPlayFabUserRequest
//        {
//            Email = email,
//            Password = password,
//            RequireBothUsernameAndEmail = false
//        };

//        PlayFabClientAPI.RegisterPlayFabUser(request,
//            result => SetDisplayName(username, phoneNumber, onSuccess, onError),
//            error => onError?.Invoke(error.GenerateErrorReport()));
//    }

//    private void SetDisplayName(string username, string phoneNumber, Action onSuccess, Action<string> onError)
//    {
//        var request = new UpdateUserTitleDisplayNameRequest { DisplayName = username };

//        PlayFabClientAPI.UpdateUserTitleDisplayName(request,
//            result =>
//            {
//                DisplayName = username;
//                SavePhoneNumber(phoneNumber, onSuccess, onError);
//            },
//            error => onError?.Invoke("Account created, but username failed: " + error.GenerateErrorReport()));
//    }

//    private void SavePhoneNumber(string phoneNumber, Action onSuccess, Action<string> onError)
//    {
//        if (string.IsNullOrEmpty(phoneNumber))
//        {
//            IsLoggedIn = true;
//            OnLoginSuccess?.Invoke();
//            onSuccess?.Invoke();
//            return;
//        }

//        var request = new UpdateUserDataRequest
//        {
//            Data = new Dictionary<string, string> { { "PhoneNumber", phoneNumber } }
//        };

//        PlayFabClientAPI.UpdateUserData(request,
//            result =>
//            {
//                IsLoggedIn = true;
//                OnLoginSuccess?.Invoke();
//                onSuccess?.Invoke();
//            },
//            error => onError?.Invoke("Account created, but phone number failed to save: " + error.GenerateErrorReport()));
//    }

//    public void Login(string email, string password, Action onSuccess, Action<string> onError)
//    {
//        var request = new LoginWithEmailAddressRequest
//        {
//            Email = email,
//            Password = password,
//            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
//            {
//                GetPlayerProfile = true,
//                ProfileConstraints = new PlayerProfileViewConstraints { ShowDisplayName = true }
//            }
//        };

//        PlayFabClientAPI.LoginWithEmailAddress(request,
//            result =>
//            {
//                DisplayName = result.InfoResultPayload?.PlayerProfile?.DisplayName;
//                IsLoggedIn = true;
//                OnLoginSuccess?.Invoke();
//                onSuccess?.Invoke();
//            },
//            error => onError?.Invoke(error.GenerateErrorReport()));
//    }

//    public void Logout()
//    {
//        if (!IsLoggedIn) return;

//        PlayFabClientAPI.ForgetAllCredentials();
//        IsLoggedIn = false;
//        DisplayName = null;
//        OnLogout?.Invoke();
//    }
//}


using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayFabManager : MonoBehaviour
{
    public static PlayFabManager Instance;

    public bool IsLoggedIn { get; private set; }
    public string DisplayName { get; private set; }
    public event Action OnLoginSuccess;
    public event Action OnLogout;

    private const string CustomIdKey = "PlayFabCustomId";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SubmitInfo(string username, string email, string phoneNumber, Action onSuccess, Action<string> onError)
    {
        var request = new LoginWithCustomIDRequest
        {
            CustomId = GetOrCreateCustomId(),
            CreateAccount = true
        };

        PlayFabClientAPI.LoginWithCustomID(request,
            result => SetDisplayName(username, email, phoneNumber, onSuccess, onError),
            error => onError?.Invoke(error.GenerateErrorReport()));
    }

    private string GetOrCreateCustomId()
    {
        if (PlayerPrefs.HasKey(CustomIdKey))
            return PlayerPrefs.GetString(CustomIdKey);

        string newId = Guid.NewGuid().ToString();
        PlayerPrefs.SetString(CustomIdKey, newId);
        PlayerPrefs.Save();
        return newId;
    }

    private void SetDisplayName(string username, string email, string phoneNumber, Action onSuccess, Action<string> onError)
    {
        var request = new UpdateUserTitleDisplayNameRequest { DisplayName = username };

        PlayFabClientAPI.UpdateUserTitleDisplayName(request,
            result =>
            {
                DisplayName = username;
                SaveContactInfo(email, phoneNumber, onSuccess, onError);
            },
            error => onError?.Invoke("Could not set name: " + error.GenerateErrorReport()));
    }

    private void SaveContactInfo(string email, string phoneNumber, Action onSuccess, Action<string> onError)
    {
        var data = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(email)) data["Email"] = email;
        if (!string.IsNullOrEmpty(phoneNumber)) data["PhoneNumber"] = phoneNumber;

        if (data.Count == 0)
        {
            FinishLogin(onSuccess);
            return;
        }

        var request = new UpdateUserDataRequest { Data = data };

        PlayFabClientAPI.UpdateUserData(request,
            result => FinishLogin(onSuccess),
            error => onError?.Invoke("Saved name, but contact info failed: " + error.GenerateErrorReport()));
    }

    private void FinishLogin(Action onSuccess)
    {
        IsLoggedIn = true;
        OnLoginSuccess?.Invoke();
        onSuccess?.Invoke();
    }

    public void Logout()
    {
        if (!IsLoggedIn) return;

        PlayFabClientAPI.ForgetAllCredentials();
        PlayerPrefs.DeleteKey(CustomIdKey);
        PlayerPrefs.Save();

        IsLoggedIn = false;
        DisplayName = null;
        OnLogout?.Invoke();
    }
}