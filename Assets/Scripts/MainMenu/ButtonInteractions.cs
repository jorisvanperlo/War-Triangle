using System;
using UnityEngine;
using UnityEngine.InputSystem.Processors;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ButtonInteractions : MonoBehaviour
{
    public GameObject mainMenuHold;
    public GameObject settingsMenuHold;
    public GameObject hangerMenuHold;
    public GameObject creditsMenuHold;

    public GameObject logoHold, camHold;

    public AudioSource menuMusic;
    public Scrollbar volumeSlider;

    public static int chosenPlane;

    public string sceneName;

    public void Start()
    {
        mainMenuHold.SetActive(false);
        settingsMenuHold.SetActive(false);
        hangerMenuHold.SetActive(false);
        creditsMenuHold.SetActive(false);
    }
    public void Play()
    {
        hangerMenuHold.SetActive(true);
        camHold.GetComponent<MenuCamController>().canLookAround = true;

        mainMenuHold.SetActive(false);
        logoHold.SetActive(false);
    }
    public void MainMenu()
    {
        mainMenuHold.SetActive(true);
        logoHold.SetActive(true);

        settingsMenuHold.SetActive(false);
        hangerMenuHold.SetActive(false);
        creditsMenuHold.SetActive(false);
        camHold.GetComponent<MenuCamController>().canLookAround = false;
    }
    public void Credits()
    {
        creditsMenuHold.SetActive(true);

        mainMenuHold.SetActive(false);
        logoHold.SetActive(false);
    }
    public void Settings()
    {
        settingsMenuHold.SetActive(true);

        mainMenuHold.SetActive(false);
        logoHold.SetActive(false);
    }
    public void Quit()
    {
        Application.Quit();
    }


    public void PlayGame()
    {
        chosenPlane = camHold.GetComponent<MenuCamController>().targetPosInt;
        SceneManager.LoadScene(sceneName);
    }

    public void VolumeChange()
    {
        menuMusic.volume = volumeSlider.value;
    }
}
