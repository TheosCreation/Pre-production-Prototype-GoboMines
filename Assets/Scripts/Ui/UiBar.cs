using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class UiBar : MonoBehaviour
{
    private Slider barSlider;
    public TMP_Text text;

    private void Awake()
    {
        barSlider = GetComponent<Slider>();
        text = GetComponentInChildren<TMP_Text>();
    }
    public void UpdateBar(float percentage)
    {
        barSlider.value = percentage;
    }
}
