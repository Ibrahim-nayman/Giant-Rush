using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Transform _player;
    [SerializeField] private Transform _endLine;
    [SerializeField] private Slider _slider;

    [SerializeField] private float _maxDistance;

    public void Start()
    {
        _maxDistance = GetDistance();
    }

    public void Update()
    {
        if (_player.position.y <= _maxDistance && _player.position.y <= _endLine.position.y)
        {
            float distance = 1 - (GetDistance() / _maxDistance);
            SetProgress(distance);
        }
    }

    public float GetDistance()
    {
        return Vector2.Distance(_player.position, _endLine.position);
    }

    public void SetProgress(float p)
    {
        _slider.value = p;
    }
}
