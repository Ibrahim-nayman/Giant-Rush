using UnityEngine;
using UnityEngine.UI;

public class ExtendBar : MonoBehaviour
{

    public Slider ExtendSliderBar;

    public void SetMaxExtend(int extend)
    {
        ExtendSliderBar.maxValue = extend;
        ExtendSliderBar.value = extend;
    }

    public void SetExtend(int extend)
    {
        ExtendSliderBar.value = extend;
    }
}
