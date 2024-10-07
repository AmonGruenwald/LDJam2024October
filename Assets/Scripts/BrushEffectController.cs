using UnityEngine;
using UnityEngine.UI;

public class BrushEffectController : MonoBehaviour
{
    // This is the material property name that controls the effect (e.g., "_Alpha")
    private const string ShaderAlphaProperty = "_Alpha";

    private Material materialInstance;
    private Image imageComponent;

    // A property that you can set to control the effect from 0 to 1
    [Range(0, 1)]
    public float effectValue = 0.0f;

    private void Start()
    {
        // Get the Image component on the same GameObject
        imageComponent = GetComponent<Image>();

        if (imageComponent != null && imageComponent.material != null)
        {
            // Create a unique material instance for this UI element to prevent modifying shared material
            materialInstance = new Material(imageComponent.material);
            imageComponent.material = materialInstance;
        }
        else
        {
            Debug.LogError("No Image component with a valid material found on this GameObject.");
        }
    }

    private void Update()
    {
        if (materialInstance != null)
        {
            // Set the shader's alpha value based on the effectValue (0-1)
            materialInstance.SetFloat(ShaderAlphaProperty, effectValue);
        }
    }

    // Optional: A public method to set the effect value from an external script
    public void SetEffectValue(float value)
    {
        effectValue = Mathf.Clamp01(value);  // Clamp to ensure value stays between 0 and 1
    }
}