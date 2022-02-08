using TMPro;
using UnityEngine;

public class ScoreCounter : MonoBehaviour
{
    public static int scoreValue = 0;
    public TextMeshProUGUI score;

    void Start()
    {
        score = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        score.text = "" + scoreValue;
    }
}