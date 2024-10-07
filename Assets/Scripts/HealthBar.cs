using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public GameObject fill;

    public float currentHealth;
    public float maxHealth;

    public Slider slider;

    float displayValue;

    // Update is called once per frame
    void Update()
    {
        slider.minValue = 0;
        slider.maxValue = 1;

        displayValue = Mathf.Lerp(displayValue, currentHealth / maxHealth, Time.deltaTime * 10);
        slider.value = displayValue;
        fill.SetActive(displayValue > 0.05);
    }
}
