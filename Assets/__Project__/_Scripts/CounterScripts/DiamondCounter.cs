using TMPro;
using UnityEngine;

public class DiamondCounter : MonoBehaviour
{
    public static int Value = 0;
    public TextMeshProUGUI score;

    void Start()
    {
        score = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        
        score.text = "" + Value;
    }
}
