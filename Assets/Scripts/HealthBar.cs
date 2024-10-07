using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public float currentHealth;
    public float maxHealth;

    public Slider slider;

    // Update is called once per frame
    void Update()
    {
        slider.value = currentHealth / maxHealth;
        slider.minValue = 0;
        slider.maxValue = 1;
    }
}
