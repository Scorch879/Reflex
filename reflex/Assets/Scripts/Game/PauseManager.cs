using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }
    public PlayerInput playerInput;
    private InputAction pauseAction;
    private bool isPaused = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        TryBindPauseAction();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetPauseState();
        TryBindPauseAction();
    }

    private void TryBindPauseAction()
    {
        pauseAction = null;
        playerInput = null;

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            return;
        }

        playerInput = playerObj.GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            Debug.LogWarning("PauseManager could not find PlayerInput on the Player object.");
            return;
        }

        pauseAction = playerInput.actions.FindAction("Pause");
        if (pauseAction == null)
        {
            Debug.LogWarning("PauseManager could not find the Pause input action.");
        }
    }

    private void Update()
    {
        TogglePause();
    }

    public void TogglePause()
    {
        if (pauseAction != null && pauseAction.WasPressedThisFrame())
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        if (InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.ShowPauseUI();
        }
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.HidePauseUI();
        }
    }

    public void ResetPauseState()
    {
        isPaused = false;
        Time.timeScale = 1f;
    }
}
