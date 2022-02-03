using System;
using NaughtyAttributes;
using RayFire;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField, BoxGroup("Game Settings")] public float CharacterSpeed = 15f;
    [SerializeField, BoxGroup("Game Settings")] private float _sideMovementSensitivity = 4f;
    [SerializeField, BoxGroup("Game Settings")] private float _sideMovementLerpSpeed = 20f;
    
    [SerializeField, BoxGroup("Setup")] private Transform _sideMovementRoot;
    [SerializeField, BoxGroup("Setup")] private Transform _leftLimit, _rightLimit;
    [SerializeField, BoxGroup("Setup")] private Camera _camera;
    [SerializeField, BoxGroup("Setup")] private Transform _stickmanExtend;
    
    [SerializeField, BoxGroup("Animator")] private Animator _animator;

    [SerializeField, BoxGroup("PlayerMaterial")] private Material _colorMat;
    [SerializeField, BoxGroup("PlayerMaterial")] private Color _yellowColor;
    [SerializeField, BoxGroup("PlayerMaterial")] private Color _greenColor;
    [SerializeField, BoxGroup("PlayerMaterial")] private Color _orangeColor;

    private Vector2 MousePositionCm
    {
        get
        {
            Vector2 pixels = Input.mousePosition;
            var inches = pixels / Screen.dpi;
            var centimeters = inches * 2.54f;

            return centimeters;
        }
    }

    private Vector2 _inputDrag;

    private Vector2 _previousMousePosition;
    private float LeftLimitX => _leftLimit.localPosition.x;
    private float RightLimitX => _rightLimit.localPosition.x;

    private float _sideMovementTarget = 0f;

    private bool _isCharacterInteract;

    private void Start()
    {
        SetLastTag();
    }

    private void Update()
    {
        switch (GameManager.Instance.CurrentGameState)
        {
            case GameState.BeforeStartGame:
                _animator.SetBool("Idle", true);
                break;
            case GameState.PlayGame:
                _animator.SetBool("Idle", false);
                _animator.SetBool("Run", true);
                HandleForwardMovement();
                HandleSideMovement();
                HandleInput();
                break;
            case GameState.WinGame:
                break;
            case GameState.LoseGame:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    #region CharacterMovement

    private void HandleForwardMovement()
    {
        transform.position += transform.forward * Time.deltaTime * CharacterSpeed;
    }

    private void HandleSideMovement()
    {
        if (!_isCharacterInteract)
        {
            _sideMovementTarget += _inputDrag.x * _sideMovementSensitivity;
            _sideMovementTarget = Mathf.Clamp(_sideMovementTarget, LeftLimitX, RightLimitX);

            var localPos = _sideMovementRoot.localPosition;

            localPos.x = Mathf.Lerp(localPos.x, _sideMovementTarget, Time.deltaTime * _sideMovementLerpSpeed);

            _sideMovementRoot.localPosition = localPos;
        }
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _previousMousePosition = MousePositionCm;
        }

        if (Input.GetMouseButton(0))
        {
            var deltaMouse = MousePositionCm - _previousMousePosition;
            _inputDrag = deltaMouse;
            _previousMousePosition = MousePositionCm;
        }
        else
        {
            _inputDrag = Vector2.zero;
        }
    }

    #endregion


    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Wall"))
        {
            other.GetComponent<RayfireRigid>().Initialize();
            _animator.SetBool("Punch", true);
        }

        if (other.CompareTag("ColorChangeOrange"))
        {
            transform.gameObject.tag = "OrangeStickman";
            _colorMat.color = _orangeColor;
        }

        if (other.CompareTag("ColorChangeYellow"))
        {
            transform.gameObject.tag = "YellowStickman";
            _colorMat.color = _yellowColor;
        }

        if (other.CompareTag("ColorChangeGreen"))
        {
            transform.gameObject.tag = "GreenStickman";
            _colorMat.color = _greenColor;
        }

        if (gameObject.CompareTag(other.tag))
        {
            Destroy(other.gameObject);
            _stickmanExtend.transform.localScale += new Vector3(0.10f, 0.10f, 0.10f);
        }

        if (!gameObject.CompareTag(other.tag) && other.CompareTag("OrangeStickman"))
        {
            Destroy(other.gameObject);
            _stickmanExtend.transform.localScale -= new Vector3(0.10f, 0.10f, 0.10f);
        }

        if (!gameObject.CompareTag(other.tag) && other.CompareTag("YellowStickman"))
        {
            Destroy(other.gameObject);
            _stickmanExtend.transform.localScale -= new Vector3(0.10f, 0.10f, 0.10f);
        }

        if (!gameObject.CompareTag(other.tag) && other.CompareTag("GreenStickman"))
        {
            Destroy(other.gameObject);
            _stickmanExtend.transform.localScale -= new Vector3(0.10f, 0.10f, 0.10f);
        }
    }

    private void SetLastTag()
    {
        if (_colorMat.color == _orangeColor)
        {
            gameObject.tag = "OrangeStickman";
        }

        if (_colorMat.color == _greenColor)
        {
            gameObject.tag = "GreenStickman";
        }

        if (_colorMat.color == _yellowColor)
        {
            gameObject.tag = "YellowStickman";
        }
    }
}