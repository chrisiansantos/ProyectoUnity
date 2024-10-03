using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System;

public class SpinnerWheel : MonoBehaviour
{
    [SerializeField]
    int segments = 37; // Fijo en 37 segmentos para la ruleta estándar europea (0-36)
    [SerializeField]
    float needleThresholdDegrees = 5f;
    [SerializeField]
    AnimationCurve curve;
    [SerializeField]
    AnimationCurve continueCurve;
    [SerializeField]
    [Range(5f, 10f)]
    float duration = 5f;
    [SerializeField]
    [Range(0f, 1f)]
    float durationRandomness = .5f;
    [SerializeField]
    UnityEngine.UI.Image wedge;
    [SerializeField]
    TextMeshProUGUI wedgeLabel;
    [SerializeField]
    [Range(0f, .01f)]
    float border;
    public UnityEvent OnSpin;
    public UnityEvent<int> OnResult;

    private bool _isSpinning = false;
    public bool IsSpinning => _isSpinning;

    private SpinnerWheelInputHandler _spinCollider;
    private Canvas _rootCanvas;
    private Canvas RootCanvas
    {
        get
        {
            if (_rootCanvas == null)
            {
                _rootCanvas = GetComponentInParent<Canvas>();
            }
            return _rootCanvas;
        }
    }
    public int Segments
    {
        get => segments;
        set
        {
            if (value == segments) return;
            segments = value;
            Draw();
        }
    }

    private void Start()
    {
        Draw();
    }

    private SpinnerWheelInputHandler SpinCollider
    {
        get
        {
            if (_spinCollider == null)
            {
                _spinCollider = GetComponentInChildren<SpinnerWheelInputHandler>();
            }
            return _spinCollider;
        }
    }

    // Calcula el offset para centrar los wedges correctamente
    private float Offset => Mathf.DeltaAngle(0f, 360f / segments) / 2f;

    public void Draw()
    {
        wedge.gameObject.SetActive(false);
        wedgeLabel.gameObject.SetActive(false);
        SpinCollider.transform.localEulerAngles = Vector3.zero;

        // Eliminar los anteriores wedges antes de dibujar nuevos
        for (int i = wedge.transform.parent.childCount - 1; i >= 0; i--)
        {
            Transform child = wedge.transform.parent.GetChild(i);
            if (child.gameObject != wedge.gameObject && child.gameObject != wedgeLabel.gameObject)
            {
                Destroy(child.gameObject);
            }
        }

        // Dibujar los wedges fijos
        for (int i = 0; i < segments; i++)
        {
            float borderAngle = Mathf.Lerp(0f, 180f, border);
            float angle = ((360f / segments) * i) + borderAngle;
            Color color = (i == 0) ? Color.green : (i % 2 == 0 ? Color.red : Color.black); // Cambiar el 0 a verde

            UnityEngine.UI.Image thisWedge = Instantiate(wedge, wedge.transform.parent);
            TextMeshProUGUI thisLabel = Instantiate(wedgeLabel, wedgeLabel.transform.parent);

            // Alineación adecuada de los wedges
            thisLabel.transform.position = wedgeLabel.transform.position;
            thisWedge.transform.localEulerAngles = new Vector3(0, 0, Offset - borderAngle);
            thisWedge.color = color;
            thisWedge.fillAmount = (1f / segments) - border;  // Ajuste para que el wedge no se deforme
            thisWedge.transform.localPosition = wedge.transform.localPosition;
            thisWedge.gameObject.SetActive(true);
            thisLabel.SetText(i.ToString());
            thisLabel.transform.SetParent(thisWedge.transform);
            thisLabel.gameObject.SetActive(true);

            // Ajustar la orientación del wedge para que se dibuje correctamente
            thisWedge.transform.localEulerAngles = new Vector3(0, 0, Offset - angle);
        }
    }

    private float Radius
    {
        get
        {
            RectTransform spinner = SpinCollider.GetComponent<RectTransform>();
            return (spinner.rect.width * .5f) * RootCanvas.scaleFactor;
        }
    }

    public void Spin(float spinStrength)
    {
        if (_isSpinning) return;
        StartCoroutine(SpinRoutine(spinStrength));
    }
    public void ContinueSpin(float spinStrength)
    {
        if (_isSpinning) return;
        StartCoroutine(SpinRoutine(spinStrength, true));
    }

    private IEnumerator SpinRoutine(float strength, bool startWithStrength = false)
    {
        _isSpinning = true;
        OnSpin?.Invoke();
        float posRandom = Mathf.Abs(durationRandomness);
        float spinDuration = duration + UnityEngine.Random.Range(posRandom * -1f, posRandom);
        float elapsed = 0f;
        Transform spinner = SpinCollider.transform;

        // Animación de la ruleta
        while (elapsed < spinDuration)
        {
            float currentZ = spinner.localEulerAngles.z;
            float curveVal = startWithStrength ? continueCurve.Evaluate(Mathf.InverseLerp(0f, spinDuration, elapsed)) : curve.Evaluate(Mathf.InverseLerp(0f, spinDuration, elapsed));
            spinner.localEulerAngles = new Vector3(0, 0, currentZ - (curveVal * strength));
            elapsed += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        // Determinar el punto más cercano a la aguja
        Vector3[] positions = new Vector3[segments];
        for (int i = 0; i < segments; i++)
        {
            float angle = (360f / segments) * i - spinner.localEulerAngles.z + Offset;
            positions[i] = spinner.position + new Vector3(Mathf.Sin(Mathf.Deg2Rad * angle), Mathf.Cos(Mathf.Deg2Rad * angle), 0f).normalized * Radius;
        }

        Vector3 top = spinner.position + Vector3.up * Radius;
        int nearestIndex = GetNearestPoint(top, positions);
        int result = nearestIndex;

        if (top.x > positions[nearestIndex].x)
        {
            result = (result + 1) % segments;
            float distanceFromNeedle = Vector3.SignedAngle(Vector3.up, spinner.position - positions[nearestIndex], Vector3.back);
            if (distanceFromNeedle > needleThresholdDegrees)
            {
                float adjustmentTime = .5f;
                float adjustmentElapsed = 0f;
                float startAngle = spinner.localEulerAngles.z;
                float targetAngle = Mathf.DeltaAngle(0f, spinner.localEulerAngles.z + needleThresholdDegrees);
                while (adjustmentElapsed < adjustmentTime)
                {
                    adjustmentElapsed += Time.deltaTime;
                    float scrub = Mathf.InverseLerp(0f, adjustmentTime, adjustmentElapsed);
                    float thisAngle = Mathf.LerpAngle(startAngle, targetAngle, scrub);
                    spinner.localEulerAngles = new Vector3(0, 0, thisAngle);
                    yield return new WaitForEndOfFrame();
                }
            }
        }

        OnResult?.Invoke(result);
        _isSpinning = false;
    }

    private int GetNearestPoint(Vector3 reference, Vector3[] targets)
    {
        int nearestIndex = -1;
        float minDist = Mathf.Infinity;
        for (int i = 0; i < targets.Length; i++)
        {
            float dist = Vector3.Distance(reference, targets[i]);
            if (dist < minDist)
            {
                nearestIndex = i;
                minDist = dist;
            }
        }
        return nearestIndex;
    }
}
