using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(TMP_InputField))]
public class HidePlaceholderOnSelect : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI placeholderText;

    private TMP_InputField inputField;

    private void Awake()
    {
        inputField = GetComponent<TMP_InputField>();

        if (placeholderText == null)
        {
            Debug.LogError("Placeholder Text is not assigned in the inspector");
            return;
        }

        inputField.onSelect.AddListener(HidePlaceholder);
        inputField.onDeselect.AddListener(ShowPlaceholder);
    }

    private void OnDestroy()
    {
        inputField.onSelect.RemoveListener(HidePlaceholder);
        inputField.onDeselect.RemoveListener(ShowPlaceholder);
    }

    private void HidePlaceholder(string text)
    {
        placeholderText.enabled = false;
    }

    private void ShowPlaceholder(string text)
    {
        if (string.IsNullOrEmpty(inputField.text))
        {
            placeholderText.enabled = true;
        }
    }
}
