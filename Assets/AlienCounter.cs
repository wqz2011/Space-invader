using UnityEngine;
using TMPro;

public class AlienCounter : MonoBehaviour
{
    TextMeshProUGUI uiText;

    void Awake()
    {
        uiText = GetComponent<TextMeshProUGUI>();
    }

    // Called by the fleet only when the count actually changes, instead of rebuilding the string every frame.
    public void SetCount(int count)
    {
        uiText.text = "Remaining Aliens: " + count.ToString("#,0");
    }
}
