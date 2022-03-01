using TMPro;
using UnityEngine;

public class DiamondCounter : MonoBehaviour
{
    public static int Value = 0;
    public TextMeshProUGUI Score;

    private void Start()
    {
        Score = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        Score.text = (PlayerPrefs.GetInt("Diamond") + Value).ToString();
    }
}