using System;
using System.Collections;
using System.Numerics;
using DG.Tweening;
using NaughtyAttributes;
using RayFire;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class PlayerController : MonoBehaviour
{
    [SerializeField, BoxGroup("Game Settings")] private float CharacterSpeed = 25f;
    [SerializeField, BoxGroup("Game Settings")] private float WallHealth = 4f;
    [SerializeField, BoxGroup("Game Settings")] private float _sideMovementSensitivity = 5f;
    [SerializeField, BoxGroup("Game Settings")] private float _sideMovementLerpSpeed = 10f;
    
    [SerializeField, BoxGroup("Slider Settings")] public ExtendBar extendBar;
    [SerializeField, BoxGroup("Slider Settings")] public HealthBar characterHealthBar;
    [SerializeField, BoxGroup("Slider Settings")] public HealthBar enemyHealthBar;
    
    [SerializeField, BoxGroup("Stickman Fight Settings")] private float CharacterHealth = 2f;
    [SerializeField, BoxGroup("Stickman Fight Settings")] private float EnemyHealth = 3f;
    [SerializeField, BoxGroup("Stickman Fight Settings")] private Vector3 _heightValue = new Vector3(0.1f,0.1f,0.1f);
    [SerializeField, BoxGroup("Stickman Fight Settings")] private Vector3 _stickmanFightPos = new Vector3();
    [SerializeField, BoxGroup("Stickman Fight Settings")] private float _punchPower = 0.2f;
    [SerializeField, BoxGroup("Stickman Fight Settings")] private float _healthValue = 0.2f;

    [SerializeField, BoxGroup("Setup")] private Transform _sideMovementRoot;
    [SerializeField, BoxGroup("Setup")] private Transform _leftLimit, _rightLimit;
    [SerializeField, BoxGroup("Setup")] private Camera _camera;
    [SerializeField, BoxGroup("Setup")] private Transform _stickmanExtend;
    [SerializeField, BoxGroup("Setup")] private GameObject _crown;
    
    [SerializeField, BoxGroup("Slider")] private GameObject _characterHealthSlider;
    [SerializeField, BoxGroup("Slider")] private GameObject _enemyHealthSlider;

    [SerializeField, BoxGroup("Animators")] private Animator _characterAnimator;
    [SerializeField, BoxGroup("Animators")] private Animator _enemyAnimator;

    [SerializeField, BoxGroup("Player Material")] private Material _colorMat;
    [SerializeField, BoxGroup("Player Material")] private Color _yellowColor;
    [SerializeField, BoxGroup("Player Material")] private Color _greenColor;
    [SerializeField, BoxGroup("Player Material")] private Color _orangeColor;

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
    private bool _isFirstRunStarted;
    private bool _isFirstBoxingIdle;
    private bool _isEnemyFight;
    private bool _isCharacterPunching;
    private bool _isCharacterHit;
    private bool _diamondInfoSaved;

    private void Start()
    {
        DiamondCounter.Value = 0;
        ScoreCounter.ScoreValue = 0;
        SetLastTag();
    }

    #region GameState

    private void Update()
    {
        RestartGame();
        
        switch (GameManager.Instance.CurrentGameState)
        {
            case GameState.BeforeStartGame:
                IdleAnimation();
                break;
            case GameState.PlayGame:
                FirstRunAnimation();
                HandleForwardMovement();
                HandleSideMovement();
                HandleInput();
                CharacterMaxHpAndExtend();
                CrownActive();
                SliderLerp();
                break;
            case GameState.FightGame:
                SliderLerp();
                FightGameState();
                GameManager.Instance.PlayGameMenu.SetActive(false);
                break;
            case GameState.WinGame:
                WinAnimation();
                SaveDiamondInfo();
                SliderOnOff();
                break;
            case GameState.LoseGame:
                SliderOnOff();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    #endregion

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
        if (other.CompareTag("FightLine"))
        {
            GameManager.Instance.CurrentGameState = GameState.FightGame;
        }

        #region Obstacle

        if (other.CompareTag("Obstacle"))
        {
            DeathAnimation();
            GameManager.Instance.Lose();
        }

        if (other.CompareTag("Wall"))
        {
            if (CharacterHealth > WallHealth)
            {
                StartCoroutine(WallPunch(other));
                
            }
            else
            {
                GameManager.Instance.Lose();
                DeathAnimation();
            }
        }

        #endregion

        #region ColorChange

        if (other.CompareTag("ColorChangeOrange"))
        {
            transform.gameObject.tag = "OrangeStickman";
            _colorMat.color = _orangeColor;
            GameManager.Instance.GreenStickmanImage.SetActive(false);
            GameManager.Instance.OrangeStickmanImage.SetActive(true);
            GameManager.Instance.YellowStickmanImage.SetActive(false);
        }

        if (other.CompareTag("ColorChangeYellow"))
        {
            transform.gameObject.tag = "YellowStickman";
            _colorMat.color = _yellowColor;
            GameManager.Instance.GreenStickmanImage.SetActive(false);
            GameManager.Instance.OrangeStickmanImage.SetActive(false);
            GameManager.Instance.YellowStickmanImage.SetActive(true);
        }

        if (other.CompareTag("ColorChangeGreen"))
        {
            transform.gameObject.tag = "GreenStickman";
            _colorMat.color = _greenColor;
            GameManager.Instance.GreenStickmanImage.SetActive(true);
            GameManager.Instance.OrangeStickmanImage.SetActive(false);
            GameManager.Instance.YellowStickmanImage.SetActive(false);
        }

        #endregion

        #region StickmanExtend

        if (gameObject.CompareTag(other.tag))
        {
            other.gameObject.SetActive(false);
            CharacterHealth += _healthValue;
            _stickmanExtend.transform.localScale += _heightValue;
            ScoreCounter.ScoreValue += 1;
        }

        if (!gameObject.CompareTag(other.tag) && other.CompareTag("OrangeStickman"))
        {
            other.gameObject.SetActive(false);
            CharacterHealth -= _healthValue;
            _stickmanExtend.transform.localScale -= _heightValue;
        }

        if (!gameObject.CompareTag(other.tag) && other.CompareTag("YellowStickman"))
        {
            other.gameObject.SetActive(false);
            CharacterHealth -= _healthValue;
            _stickmanExtend.transform.localScale -= _heightValue;
        }

        if (!gameObject.CompareTag(other.tag) && other.CompareTag("GreenStickman"))
        {
            other.gameObject.SetActive(false);
            CharacterHealth -= _healthValue;
            _stickmanExtend.transform.localScale -= _heightValue;
        }

        #endregion

        if (other.CompareTag("Diamond"))
        {
            other.gameObject.SetActive(false);
            DiamondCounter.Value++;
        }
    }

    #region SetLastTag

    private void SetLastTag()
    {
        if (_colorMat.color == _orangeColor)
        {
            gameObject.tag = "OrangeStickman";
            GameManager.Instance.GreenStickmanImage.SetActive(false);
            GameManager.Instance.OrangeStickmanImage.SetActive(true);
            GameManager.Instance.YellowStickmanImage.SetActive(false);
        }

        if (_colorMat.color == _greenColor)
        {
            gameObject.tag = "GreenStickman";
            GameManager.Instance.GreenStickmanImage.SetActive(true);
            GameManager.Instance.OrangeStickmanImage.SetActive(false);
            GameManager.Instance.YellowStickmanImage.SetActive(false);
        }

        if (_colorMat.color == _yellowColor)
        {
            gameObject.tag = "YellowStickman";
            GameManager.Instance.GreenStickmanImage.SetActive(false);
            GameManager.Instance.OrangeStickmanImage.SetActive(false);
            GameManager.Instance.YellowStickmanImage.SetActive(true);
        }
    }

    #endregion

    private void RestartGame()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            GameManager.Instance.LoadReachedLevel();
            GameManager.Instance.CurrentGameState = GameState.BeforeStartGame;
        }
    }

    private IEnumerator WallPunch(Collider other)
    {
        _isCharacterInteract = true;
        CharacterSpeed = 0f;
        PunchAnimation();
        yield return new WaitForSeconds(0.75f);
        _isCharacterInteract = false;
        other.transform.parent.GetComponentInChildren<RayfireRigid>().Initialize();
        _camera.transform.DOShakePosition(0.75f);
        CharacterSpeed = 35;
        CharacterHealth -= _healthValue * 10;
        _stickmanExtend.localScale -= _heightValue * 10;
        RunAfterWallAnimation();
    }

    private IEnumerator FightPunchDelay()
    {
        if (Input.GetMouseButtonDown(0) && !_isCharacterPunching)
        {
            _isCharacterHit = true;
            _isCharacterPunching = true;
            PunchAnimation();

            yield return new WaitForSeconds(0.5f);

            EnemyHitAnimation();
            EnemyHealth -= _punchPower;

            yield return new WaitForSeconds(0.5f);

            _enemyAnimator.SetBool("EnemyHit", false);
            BoxingIdle();

            yield return new WaitForSeconds(0.5f);
            _isCharacterHit = false;
            _isCharacterPunching = false;
        }
    }

    private IEnumerator EnemyFightPunchDelay()
    {
        if (!_isEnemyFight && !_isCharacterHit)
        {
            _isEnemyFight = true;
            EnemyBoxing();

            yield return new WaitForSeconds(0.5f);

            if (!_isCharacterHit)
            {
                HitAnimation();
                CharacterHealth -= _punchPower;
            }

            yield return new WaitForSeconds(0.5f);

            if (!_isCharacterHit)
            {
                _characterAnimator.SetBool("Hit", false);
                EnemyIdle();
            }

            yield return new WaitForSeconds(1f);

            _isEnemyFight = false;
        }
    }

    private void CharacterMaxHpAndExtend()
    {
        if (_stickmanExtend.localScale.y <= 2f)
        {
            _stickmanExtend.localScale = new Vector3(2f, 2f, 2f);
            CharacterHealth = 2f;
        }

        if (_stickmanExtend.localScale.y >= 5f)
        {
            _stickmanExtend.localScale = new Vector3(5f, 5f, 5f);
            CharacterHealth = 5f;
        }
    }

    public void FightGameState()
    {
        StartCoroutine(SliderDelay());

        gameObject.transform.position = Vector3.Lerp(gameObject.transform.position, _stickmanFightPos, 2 * Time.deltaTime);
        _sideMovementRoot.transform.localPosition = Vector3.Lerp(_sideMovementRoot.transform.localPosition, Vector3.zero, 2* Time.deltaTime);
        _camera.transform.localPosition = Vector3.Lerp(_camera.transform.localPosition, new Vector3(15, 8, 1.5f), 2 * Time.deltaTime);
        _camera.transform.localRotation = Quaternion.Lerp(_camera.transform.localRotation, Quaternion.Euler(15, -90, 0), 2 * Time.deltaTime);
        
        CharacterSpeed = 0;
        _isCharacterInteract = true;

        if (!_isFirstBoxingIdle)
        {
            _isFirstBoxingIdle = true;
            BoxingIdle();
            characterHealthBar.HealthSliderBar.maxValue = CharacterHealth;
            characterHealthBar.HealthSliderBar.value = characterHealthBar.HealthSliderBar.maxValue;
            enemyHealthBar.HealthSliderBar.maxValue = EnemyHealth;
            enemyHealthBar.HealthSliderBar.value = enemyHealthBar.HealthSliderBar.maxValue;
        }
        
        StartCoroutine(FightPunchDelay());

        StartCoroutine(EnemyFightPunchDelay());

        if (EnemyHealth <= 0)
        {
            EnemyDeathAnimation();
            GameManager.Instance.Win();
        }

        if (CharacterHealth <= 0)
        {
            DeathAnimation();
            GameManager.Instance.Lose();
        }
    }

    private void IdleAnimation()
    {
        _characterAnimator.SetBool("Idle", true);
        _characterAnimator.SetBool("Run", false);
    }

    private void RunAfterWallAnimation()
    {
        _characterAnimator.SetBool("Run", true);
        _characterAnimator.SetBool("Idle", false);
        _characterAnimator.SetBool("Punch", false);
    }

    private void FirstRunAnimation()
    {
        if (!_isFirstRunStarted)
        {
            _isFirstRunStarted = true;
            _characterAnimator.SetBool("Run", true);
            _characterAnimator.SetBool("Idle", false);
            _characterAnimator.SetBool("Win", false);
        }
    }

    private void DeathAnimation()
    {
        _characterAnimator.SetBool("Death", true);
        _characterAnimator.SetBool("Run", false);
        _characterAnimator.SetBool("BoxingIdle", false);
    }

    private void EnemyDeathAnimation()
    {
        _enemyAnimator.SetBool("Death", true);
        _enemyAnimator.SetBool("Idle", false);
        _enemyAnimator.SetBool("Fight", false);
    }

    private void WinAnimation()
    {
        _characterAnimator.SetBool("Win", true);
        _characterAnimator.SetBool("BoxingIdle", false);
    }

    private void PunchAnimation()
    {
        _characterAnimator.SetBool("Punch", true);
        _characterAnimator.SetBool("Run", false);
        _characterAnimator.SetBool("BoxingIdle", false);
    }

    private void BoxingIdle()
    {
        _characterAnimator.SetBool("BoxingIdle", true);
        _characterAnimator.SetBool("Punch", false);
        _characterAnimator.SetBool("Run", false);
    }

    private void EnemyBoxing()
    {
        _enemyAnimator.SetBool("Fight", true);
        _enemyAnimator.SetBool("Idle", false);
    }

    private void EnemyIdle()
    {
        _enemyAnimator.SetBool("Idle", true);
        _enemyAnimator.SetBool("Fight", false);
    }

    private void HitAnimation()
    {
        _characterAnimator.SetBool("Hit", true);
    }

    private void EnemyHitAnimation()
    {
        _enemyAnimator.SetBool("EnemyHit", true);
    }

    private void SaveDiamondInfo()
    {
        if (!_diamondInfoSaved)
        {
            _diamondInfoSaved = true;
            PlayerPrefs.SetInt("Diamond",DiamondCounter.Value + PlayerPrefs.GetInt("Diamond"));
        }
    }

    private void SliderOnOff()
    {
        _characterHealthSlider.SetActive(false);
        _enemyHealthSlider.SetActive(false);
    }

    private IEnumerator SliderDelay()
    {
        yield return new WaitForSeconds(0.5f);
        _characterHealthSlider.SetActive(true);
        _enemyHealthSlider.SetActive(true);
    }

    private void CrownActive()
    {
        if (CharacterHealth >= 4.5f)
        {
            _crown.SetActive(true);
        }
        else
        {
            _crown.SetActive(false);
        }
    }

    private void SliderLerp()
    {
        extendBar.ExtendSliderBar.value = Mathf.Lerp(extendBar.ExtendSliderBar.value, _stickmanExtend.localScale.y, 2 * Time.deltaTime);
        characterHealthBar.HealthSliderBar.value = Mathf.Lerp(characterHealthBar.HealthSliderBar.value, CharacterHealth,2 * Time.deltaTime);
        enemyHealthBar.HealthSliderBar.value = Mathf.Lerp(enemyHealthBar.HealthSliderBar.value, EnemyHealth,2 * Time.deltaTime);
    }
}
