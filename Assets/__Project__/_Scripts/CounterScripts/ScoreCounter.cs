using TMPro;
using UnityEngine;

public class ScoreCounter : MonoBehaviour
{
    public static int ScoreValue = 0;
    public TextMeshProUGUI Score;

    private void Start()
    {
        Score = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        Score.text = "" + ScoreValue;
    }
}