using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AssetCardElements : MonoBehaviour
{
    [SerializeField]
    private Image assetImage;

    [SerializeField]
    private TextMeshProUGUI assetNameLabel;

    [SerializeField]
    private TextMeshProUGUI assetCountLabel;

    [SerializeField]
    public Button selectButton;

    public Image AssetImage => assetImage;
    public TextMeshProUGUI AssetNameLabel => assetNameLabel;
    public TextMeshProUGUI AssetCountLabel => assetCountLabel;

    // If needed, initialize components here
    void Start()
    {

    }

    // If needed, update components here
    void Update()
    {

    }
}
