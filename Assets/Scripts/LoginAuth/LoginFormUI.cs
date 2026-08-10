using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoginFormUI : MonoBehaviour
{
    [Header("Fields")]
    public TMP_InputField usernameField;
    public TMP_InputField emailField;
    public TMP_InputField phoneField;
    public Button loginButton;
    public TMP_Text errorText;

    [Header("Required Fields")]
    public bool usernameRequired = true;
    public bool emailRequired = true;
    public bool phoneRequired = false;

    [Header("Colors")]
    public Color successColor = Color.green;
    public Color errorColor = Color.red;

    void Start()
    {
        loginButton.onClick.AddListener(OnLoginClicked);
    }

    void OnEnable()
    {
        if (errorText != null) errorText.text = "";
        if (usernameField != null) usernameField.text = "";
        if (emailField != null) emailField.text = "";
        if (phoneField != null) phoneField.text = "";
    }

    private void OnLoginClicked()
    {
        errorText.text = "";

        string username = usernameField.text.Trim();
        string email = emailField.text.Trim();
        string phoneNumber = phoneField.text.Trim();

        if (usernameRequired && string.IsNullOrEmpty(username))
        {
            ShowError("Enter a name.");
            return;
        }
        if (emailRequired && string.IsNullOrEmpty(email))
        {
            ShowError("Enter an email.");
            return;
        }
        if (phoneRequired && string.IsNullOrEmpty(phoneNumber))
        {
            ShowError("Enter a phone number.");
            return;
        }

        PlayFabManager.Instance.SubmitInfo(username, email, phoneNumber,
            onSuccess: () =>
            {
                ShowSuccess("Successfully logged in!");
                ShowNext();
            },
            onError: err => ShowError("Something went wrong: " + err));
    }

    private void ShowSuccess(string message)
    {
        errorText.color = successColor;
        errorText.text = message;
    }

    private void ShowError(string message)
    {
        errorText.color = errorColor;
        errorText.text = message;
    }

    private void ShowNext()
    {
        if (MenuManager.instance != null) MenuManager.instance.CloseCurrectPanel();
        else if (GameManager.Instance != null) GameManager.Instance.CloseCurrectPanel();
        else if (UIManager.Instance != null) UIManager.Instance.CloseCurrectPanel();
    }
}