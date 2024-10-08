using UnityEngine.UI;
using UnityEngine;

[RequireComponent(typeof(Slider))]
public class SliderHandler : MonoBehaviour
{
    [SerializeField]
    SpinnerWheel spinner;
    [SerializeField]
    TMPro.TextMeshProUGUI valueReadout;
    Slider _slider;

    void Start()
    {
        _slider = GetComponent<Slider>();
        _slider.onValueChanged.AddListener((f) =>
        {
            int value = Mathf.RoundToInt(f);
            valueReadout.text = value.ToString();
        });
    }

    public void SetSpinStrength(float value)
    {
        spinner.Spin(value);
    }
}
