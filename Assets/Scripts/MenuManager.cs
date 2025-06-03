using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public GameObject menuPanel;
    public KeyCode toggleKey = KeyCode.Tab;

    private bool isMenuOpen = false;
    private PlayerController _playerController;

    void Start()
    {
        menuPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Find the local player's PlayerController script
        _playerController = FindFirstObjectByType<PlayerController>();
        if (_playerController != null && !_playerController.IsOwner)
        {
            foreach (var player in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            {
                if (player.IsOwner)
                {
                    _playerController = player;
                    break;
                }
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            isMenuOpen = !isMenuOpen;
            menuPanel.SetActive(isMenuOpen);
            // Enable/disable camera rotation
            if (_playerController != null)
            {
                _playerController.SetCameraRotationEnabled(!isMenuOpen);
            }

            // Enable/disable cursor for menu navigation
            if (isMenuOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}