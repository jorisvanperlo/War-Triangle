using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public string sceneName;
    public bool pause;
    public GameObject menuButton;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            pause = !pause;
            pauseOrResume();
        }
    }
    public void pauseOrResume()
    {
        if (pause)
        {
            Time.timeScale = 0.0f;
            menuButton.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (!pause)
        {
            Time.timeScale = 1.0f;
            menuButton.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    public void MainMenu()
    {
        Time.timeScale = 1.0f;
        SceneManager.LoadScene(sceneName);
    }
}
