using TMPro;
using UnityEngine;

public class TypewriterFloatControl : MonoBehaviour
{
    [Range(0, 1)] public float revealProgress = 1; // Controls text reveal from 0 (invisible) to 1 (fully visible)
    public TextMeshProUGUI textMeshPro; // Reference to the TextMeshPro component
    private string fullText;

    private void OnEnable()
    {
        if (textMeshPro == null)
            textMeshPro = GetComponent<TextMeshProUGUI>();

        fullText = textMeshPro.text;
    }

    private void Update()
    {
        // Clamp reveal progress to prevent out-of-bounds issues
        revealProgress = Mathf.Clamp01(revealProgress);

        int totalCharacters = fullText.Length;
        int visibleCharacters = Mathf.FloorToInt(totalCharacters * revealProgress);

        // Construct the text with revealed and transparent portions
        textMeshPro.text = $"{fullText.Substring(0, visibleCharacters)}<color=#00000000>{fullText.Substring(visibleCharacters)}</color>";
    }
}