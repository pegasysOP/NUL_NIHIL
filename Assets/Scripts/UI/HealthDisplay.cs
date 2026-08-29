using TMPro;
using UnityEngine;


[DisallowMultipleComponent]
public class HealthDisplay : MonoBehaviour
{
    [SerializeField] private PlayerHealth health;
    [SerializeField] private TextMeshProUGUI label;

    private void Start()
    {
        health.HealthChanged += OnHealthChanged;
        OnHealthChanged(health.Current);
    }

    private void OnHealthChanged(int current)
    {
        label.text = current.ToString();
    }
}
