using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuManager : MonoBehaviour
{
    public GameObject[] Tabs;
    public Image[] TabButtons;
    public Color InactiveTabColor = Color.gray;
    public Color ActiveTabColor = Color.white;
    public Canvas canvas;
    public KeyCode toggleKey = KeyCode.Tab;
    public WalletLogin walletLogin;

    private PlayerController _playerController;
    private bool _isOwnerInstance = false; // Flag to track if this instance belongs to the local player

    void Start()
    {
        // Attempt to get the PlayerController this MenuManager is associated with.
        _playerController = GetComponentInParent<PlayerController>();

        if (_playerController != null && _playerController.IsOwner)
        {
            _isOwnerInstance = true;
            if (canvas != null)
            {
                canvas.enabled = false; // Start with menu hidden for the owner.
            }
            else
            {
                Debug.LogError("[MenuManager] Canvas is not assigned!", this);
                enabled = false; // Disable script if canvas is missing
                return;
            }

            for (int i = 0; i < Tabs.Length; i++)
            {
                if (Tabs[i] != null)
                    Tabs[i].SetActive(i == 0);
                if (i < TabButtons.Length && TabButtons[i] != null)
                    TabButtons[i].color = (i == 0) ? ActiveTabColor : InactiveTabColor;
            }
        }
        else
        {
            _isOwnerInstance = false;
            // This MenuManager belongs to a remote player's representation or PlayerController isn't found/owned.
            // Ensure its canvas is disabled to prevent interference.
            if (canvas != null)
            {
                canvas.enabled = false;
            }
        }
    }

    void Update()
    {
        // Only process input if this MenuManager instance belongs to the local player
        // and its associated PlayerController is available.
        if (!_isOwnerInstance || _playerController == null)
        {
            return;
        }

        if (Input.GetKeyDown(toggleKey))
        {
            if (canvas == null) return;

            canvas.enabled = !canvas.enabled;
            _playerController.IsMenuOpen = canvas.enabled;
            if (canvas.enabled)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                _playerController.SetCameraRotationEnabled(false); // Freeze camera
            }
            else
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                _playerController.SetCameraRotationEnabled(true); // Unfreeze camera
            }
        }
    }

    public void SwitchToTab(int TabID)
    {
        if (!_isOwnerInstance) return;

        for (int i = 0; i < Tabs.Length; i++)
        {
            Tabs[i].SetActive(i == TabID);
        }
        for (int i = 0; i < TabButtons.Length; i++)
        {
            TabButtons[i].color = (i == TabID) ? ActiveTabColor : InactiveTabColor;
        }
    }
}