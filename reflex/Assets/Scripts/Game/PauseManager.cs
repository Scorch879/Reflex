using UnityEngine;
using UnityEngine.InputSystem;

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
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        GameObject playerObj = GameObject.FindWithTag("Player");
        playerInput = playerObj.GetComponent<PlayerInput>();
        pauseAction = playerInput.actions.FindAction("Pause");

    }

    private void Update()
    {
        TogglePause();
    }

    public void TogglePause()
    {
        if (pauseAction.WasPressedThisFrame())
        {
            isPaused = !isPaused;
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
        Time.timeScale = 0f;
        InGameUIManager.Instance.ShowPauseUI();
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        InGameUIManager.Instance.HidePauseUI();
    }
}
