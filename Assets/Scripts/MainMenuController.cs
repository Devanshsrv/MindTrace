using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public void LoadCorsi()
    {
        SceneManager.LoadScene("Corsi");
    }

    public void LoadGoNoGo()
    {
        SceneManager.LoadScene("Go&NoGo2");
    }

    public void LoadTest3()
    {
        SceneManager.LoadScene("MainMenuStroop");
    }

    public void QuitApp()
    {
        Application.Quit();
        Debug.Log("Quit");
    }

    public void OpenSettings()
    {
        Debug.Log("Settings clicked");
    }
}