using System;
using System.Collections;
using System.Numerics;
using NaughtyAttributes;
using RayFire;
using UnityEngine;
using UnityEngine.UIElements;
using Quaternion = UnityEngine.Quaternion;
using Random = System.Random;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class PlayerController : MonoBehaviour
{
    [SerializeField, BoxGroup("Game Settings")] public float CharacterHealth = 2f;
    [SerializeField, BoxGroup("Game Settings")] public float WallHealth;
    [SerializeField, BoxGroup("Game Settings")] public float EnemyHealth = 2f;
    [SerializeField, BoxGroup("Game Settings")] public float CharacterSpeed = 25f;
    [SerializeField, BoxGroup("Game Settings")] private Vector3 _heightValue = new Vector3(0.1f,0.1f,0.1f);
    [SerializeField, BoxGroup("Game Settings")] private Vector3 _stickmanFightPos = new Vector3();
    [SerializeField, BoxGroup("Game Settings")] private float _sideMovementSensitivity = 5f;
    [SerializeField, BoxGroup("Game Settings")] private float _sideMovementLerpSpeed = 10f;
    [SerializeField, BoxGroup("Game Settings")] private float _punchPower = 0.2f;
    [SerializeField, BoxGroup("Game Settings")] private GameObject _ui;

    [SerializeField, BoxGroup("Setup")] private Transform _sideMovementRoot;
    [SerializeField, BoxGroup("Setup")] private Transform _leftLimit, _rightLimit;
    [SerializeField, BoxGroup("Setup")] private Camera _camera;
    [SerializeField, BoxGroup("Setup")] private GameObject _fightCamera;
    [SerializeField, BoxGroup("Setup")] private Transform _stickmanExtend;

    [SerializeField, BoxGroup("Player Animator")] private Animator _animator;

    [SerializeField, BoxGroup("Enemy Animator")] private Animator _enemyAnimator;

    [SerializeField, BoxGroup("Player Material")] private Material _colorMat;
    [SerializeField, BoxGroup("Player Material")] private Color _yellowColor;
    [SerializeField, BoxGroup("Player Material")] private Color _greenColor;
    [SerializeField, BoxGroup("Player Material")] private Color _orangeColor;

    [SerializeField, BoxGroup("Stickman Image")] private GameObject _greenStickmanImage;
    [SerializeField, BoxGroup("Stickman Image")] private GameObject _orangeStickmanImage;
    [SerializeField, BoxGroup("Stickman Image")] private GameObject _yellowStickmanImage;

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
    private bool _isCharacterDeath;
    private bool _isEnemyFight;
    private bool _isCharacterPunching;

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
                break;
            case GameState.FightGame:
                FightGameState();
                break;
            case GameState.WinGame:
                WinAnimation();
                _ui.SetActive(false);
                break;
            case GameState.LoseGame:
                _ui.SetActive(false);
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
            StartCoroutine(DeathDelay());
            //TODO Ölüm animasyonu koyulacak ve retry tuşuna basınca yukarıdaki kodlar çalışacak.
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
                //TODO Ölüm animasyonu koyulacak ve retry tuşuna basınca yukarıdaki kodlar çalışacak.
            }
        }

        #endregion

        #region ColorChange

        if (other.CompareTag("ColorChangeOrange"))
        {
            transform.gameObject.tag = "OrangeStickman";
            _colorMat.color = _orangeColor;
            _greenStickmanImage.SetActive(false);
            _orangeStickmanImage.SetActive(true);
            _yellowStickmanImage.SetActive(false);
        }

        if (other.CompareTag("ColorChangeYellow"))
        {
            transform.gameObject.tag = "YellowStickman";
            _colorMat.color = _yellowColor;
            _greenStickmanImage.SetActive(false);
            _orangeStickmanImage.SetActive(false);
            _yellowStickmanImage.SetActive(true);
        }

        if (other.CompareTag("ColorChangeGreen"))
        {
            transform.gameObject.tag = "GreenStickman";
            _colorMat.color = _greenColor;
            _greenStickmanImage.SetActive(true);
            _orangeStickmanImage.SetActive(false);
            _yellowStickmanImage.SetActive(false);
        }

        #endregion

        #region StickmanExtend

        if (gameObject.CompareTag(other.tag))
        {
            other.gameObject.SetActive(false);
            CharacterHealth += 0.1f;
            _stickmanExtend.transform.localScale += _heightValue;
            ScoreCounter.ScoreValue += 1;
        }

        if (!gameObject.CompareTag(other.tag) && other.CompareTag("OrangeStickman"))
        {
            other.gameObject.SetActive(false);
            CharacterHealth -= 0.1f;
            _stickmanExtend.transform.localScale -= _heightValue;
        }

        if (!gameObject.CompareTag(other.tag) && other.CompareTag("YellowStickman"))
        {
            other.gameObject.SetActive(false);
            CharacterHealth -= 0.1f;
            _stickmanExtend.transform.localScale -= _heightValue;
        }

        if (!gameObject.CompareTag(other.tag) && other.CompareTag("GreenStickman"))
        {
            other.gameObject.SetActive(false);
            CharacterHealth -= 0.1f;
            _stickmanExtend.transform.localScale -= _heightValue;
        }

        #endregion

        if (other.CompareTag("Diamond"))
        {
            other.gameObject.SetActive(false);
            DiamondCounter.Value += 1;
        }
    }

    #region SetLastTag

    private void SetLastTag()
    {
        if (_colorMat.color == _orangeColor)
        {
            gameObject.tag = "OrangeStickman";
            _greenStickmanImage.SetActive(false);
            _orangeStickmanImage.SetActive(true);
            _yellowStickmanImage.SetActive(false);
        }

        if (_colorMat.color == _greenColor)
        {
            gameObject.tag = "GreenStickman";
            _greenStickmanImage.SetActive(true);
            _orangeStickmanImage.SetActive(false);
            _yellowStickmanImage.SetActive(false);
        }

        if (_colorMat.color == _yellowColor)
        {
            gameObject.tag = "YellowStickman";
            _greenStickmanImage.SetActive(false);
            _orangeStickmanImage.SetActive(false);
            _yellowStickmanImage.SetActive(true);
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
        CharacterSpeed = 0f;
        PunchAnimation();
        yield return new WaitForSeconds(0.75f);
        other.transform.parent.GetComponentInChildren<RayfireRigid>().Initialize();
        CharacterSpeed = 25;
        CharacterHealth -= 1;
        RunAfterWallAnimation();
    }

    private IEnumerator FightPunchDelay()
    {
        if (Input.GetMouseButtonDown(0) && !_isCharacterPunching)
        {
            _isCharacterPunching = true;
            PunchAnimation();
            yield return new WaitForSeconds(0.5f);
            EnemyHitAnimation();
            yield return new WaitForSeconds(0.5f);
            _enemyAnimator.SetBool("EnemyHit",false);
            EnemyHealth -= _punchPower;
            BoxingIdle();
            yield return new WaitForSeconds(1.2f);
            _isCharacterPunching = false;
        }
    }

    private IEnumerator EnemyFightPunchDelay()
    {
        if (!_isEnemyFight)
        {
            _isEnemyFight = true;
            EnemyBoxing();
            yield return new WaitForSeconds(0.5f);
            HitAnimation();
            yield return new WaitForSeconds(0.5f);
            _animator.SetBool("Hit",false);
            CharacterHealth -= _punchPower;
            EnemyIdle();
            yield return new WaitForSeconds(1f);
            _isEnemyFight = false;
        }
    }

    private IEnumerator DeathDelay()
    {
        if (!_isCharacterDeath)
        {
            _isCharacterDeath = true;
            DeathAnimation();
            yield return new WaitForSeconds(0.1f);
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
        _stickmanExtend.position = _stickmanFightPos;
        CharacterSpeed = 0;
        _isCharacterInteract = true;
        //_animator.applyRootMotion = true;
        //_enemyAnimator.applyRootMotion = true;

        if (!_isFirstBoxingIdle)
        {
            _isFirstBoxingIdle = true;
            BoxingIdle();
        }

        _fightCamera.SetActive(true);

        StartCoroutine(FightPunchDelay());

        StartCoroutine(EnemyFightPunchDelay());
        
        if (EnemyHealth <= 0)
        {
            GameManager.Instance.Win();
            EnemyDeathAnimation();
        }

        if (CharacterHealth <= 0)
        {
            DeathAnimation();
            GameManager.Instance.Lose();
        }
    }
    
    private void IdleAnimation()
    {
        _animator.SetBool("Idle", true);
        _animator.SetBool("Run", false);
    }

    private void RunAfterWallAnimation()
    {
        _animator.SetBool("Run", true);
        _animator.SetBool("Idle", false);
        _animator.SetBool("Punch", false);
    }
    
    private void FirstRunAnimation()
    {
        if (!_isFirstRunStarted)
        {
            _isFirstRunStarted = true;
            _animator.SetBool("Run", true);
            _animator.SetBool("Idle", false);
            _animator.SetBool("Win",false);
        }
    }

    private void DeathAnimation()
    {
        _animator.SetBool("Death", true);
        _animator.SetBool("Run", false);
        _animator.SetBool("BoxingIdle",false);
    }

    private void EnemyDeathAnimation()
    {
        _enemyAnimator.SetBool("Death",true);
        _enemyAnimator.SetBool("Idle",false);
        _enemyAnimator.SetBool("Fight",false);
    }

    private void WinAnimation()
    {
        _animator.SetBool("Win", true);
        _animator.SetBool("BoxingIdle", false);
    }

    private void PunchAnimation()
    {
        _animator.SetBool("Punch", true);
        _animator.SetBool("Run", false);
        _animator.SetBool("BoxingIdle", false);
    }

    private void BoxingIdle()
    {
        _animator.SetBool("BoxingIdle", true);
        _animator.SetBool("Punch", false);
        _animator.SetBool("Run", false);
    }

    private void EnemyBoxing()
    {
        _enemyAnimator.SetBool("Fight",true);
        _enemyAnimator.SetBool("Idle",false);
    }

    private void EnemyIdle()
    {
        _enemyAnimator.SetBool("Idle",true);
        _enemyAnimator.SetBool("Fight",false);
    }

    private void HitAnimation()
    {
        _animator.SetBool("Hit",true);
    }

    private void EnemyHitAnimation()
    {
        _enemyAnimator.SetBool("EnemyHit",true);
        
    }
}
