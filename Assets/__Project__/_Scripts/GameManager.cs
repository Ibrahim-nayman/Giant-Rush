using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using MoreMountains.NiceVibrations;
using NaughtyAttributes;
using TMPro;

public enum GameState
{
    BeforeStartGame,
    PlayGame,
    FightGame,
    WinGame,
    LoseGame
}

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance => _instance ??= FindObjectOfType<GameManager>();

    [ReadOnly] public GameState CurrentGameState;

    public GameObject BeforeStartMenu;
    public GameObject PlayGameMenu;
    public GameObject WinMenu;
    public GameObject LoseMenu;
    public GameObject GreenStickmanImage;
    public GameObject OrangeStickmanImage;
    public GameObject YellowStickmanImage;

    public int LoopAfter;

    public TextMeshProUGUI LevelNumText;

    [HideInInspector] public bool IsGameEnded;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        Application.targetFrameRate = 60;
        DontDestroyOnLoad(this.gameObject);
        LoadReachedLevel();
    }

    public void LoadReachedLevel()
    {
        MMVibrationManager.TransientHaptic(1, 0.1f, true, this);
        SceneManager.LoadScene(PlayerPrefs.GetInt("reachedLevel", 2));
        LevelNumText.text = "Level " + PlayerPrefs.GetInt("fakeLevelNumber", 1).ToString();
        WinMenu.SetActive(false);
        LoseMenu.SetActive(false);
        BeforeStartMenu.SetActive(true);
        PlayGameMenu.SetActive(true);
    }
    
    public void Win()
    {
        StartCoroutine(MenuDelay(WinMenu, 2f));
        CurrentGameState = GameState.WinGame;
        MMVibrationManager.TransientHaptic(1, 0.1f, true, this);
        PlayerPrefs.SetInt("fakeLevelNumber", PlayerPrefs.GetInt("fakeLevelNumber", 1) + 1);

        if (SceneManager.sceneCountInBuildSettings > PlayerPrefs.GetInt("reachedLevel", 2) + 1)
        {
            PlayerPrefs.SetInt("reachedLevel", PlayerPrefs.GetInt("reachedLevel", 2) + 1);
        }
        else
        {
            PlayerPrefs.SetInt("reachedLevel", LoopAfter + 1);
        }
    }

    public void Lose()
    {
        StartCoroutine(MenuDelay(LoseMenu, 2f));
        CurrentGameState = GameState.LoseGame;
        MMVibrationManager.TransientHaptic(1, 0.1f, true, this);
    }

    public void PlayGame()
    {
        CurrentGameState = GameState.PlayGame;
        BeforeStartMenu.SetActive(false);
    }

    private IEnumerator MenuDelay(GameObject menu, float time)
    {
        if (!IsGameEnded)
        {
            IsGameEnded = true;
            yield return new WaitForSeconds(time);
            menu.SetActive(true);
        }
    }
}