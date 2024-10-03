using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class SpinReport : MonoBehaviour
{
    TextMeshProUGUI text;
    private void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
        text.SetText(string.Empty);
    }

    public void Report(int result)
    {
        text.SetText($"Resultado: {result }");
    }
    public void Clear()
    {
        text.SetText(string.Empty);
    }
}
