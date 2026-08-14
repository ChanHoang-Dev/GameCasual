using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIStartScene : MonoBehaviour
{
    [SerializeField] private Button startGameBtn;
    [SerializeField] private Button optionBtn;
    [SerializeField] private Button exitBtn;
    [SerializeField] private GameObject optionPanel;
    [Header("Music")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private TextMeshProUGUI musicPercentText;

    [Header("SFX")]
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private TextMeshProUGUI sfxPercentText;

    [Header("UI")]
    [SerializeField] private Slider uiSlider;
    [SerializeField] private TextMeshProUGUI uiPercentText;
    [SerializeField] private Button closeOptionBtn;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startGameBtn.onClick.AddListener(OnStartGameClicked);
        optionBtn.onClick.AddListener(OnOptionClicked);
        exitBtn.onClick.AddListener(OnExitClicked);
        closeOptionBtn.onClick.AddListener(OnCloseOptionClicked);

        musicSlider.onValueChanged.AddListener(OnMusicSliderValueChanged);
        sfxSlider.onValueChanged.AddListener(OnSfxSliderValueChanged);
        uiSlider.onValueChanged.AddListener(OnUiSliderValueChanged);

        optionPanel.SetActive(false); 

        musicSlider.SetValueWithoutNotify(AudioManager.Instance.musicVolume);
        sfxSlider.SetValueWithoutNotify(AudioManager.Instance.sfxVolume);
        uiSlider.SetValueWithoutNotify(AudioManager.Instance.uiVolume);

        UpdatePercentText(musicPercentText, musicSlider.value);
        UpdatePercentText(sfxPercentText, sfxSlider.value);
        UpdatePercentText(uiPercentText, uiSlider.value);
    }

    private void OnCloseOptionClicked()
    {
        optionPanel.SetActive(false);
        startGameBtn.gameObject.SetActive(true);
        optionBtn.gameObject.SetActive(true);
        exitBtn.gameObject.SetActive(true);
    }

    private void OnExitClicked()
    {
        AudioManager.Instance.SaveVolumeSettings();
        Application.Quit();
    }

    private void OnOptionClicked()
    {
        optionPanel.SetActive(true);
        startGameBtn.gameObject.SetActive(false);
        optionBtn.gameObject.SetActive(false);
        exitBtn.gameObject.SetActive(false);
    }

    private void OnStartGameClicked()
    {
        SceneManager.LoadScene("MainScene");
    }

    public void OnMusicSliderValueChanged(float value)
    {
        AudioManager.Instance.SetMusicVolume(value);
        UpdatePercentText(musicPercentText, value);
    }
    public void OnSfxSliderValueChanged(float value)
    {
        AudioManager.Instance.SetSfxVolume(value);
        UpdatePercentText(sfxPercentText, value);
    }
    public void OnUiSliderValueChanged(float value)
    {
        AudioManager.Instance.SetUiVolume(value);
        UpdatePercentText(uiPercentText, value);
    }
    public void UpdatePercentText(TextMeshProUGUI text, float value)
    {
        int percent = Mathf.RoundToInt(value * 100);
        text.text = percent.ToString() + "%";
    }
    // Update is called once per frame
    void Update()
    {

    }
}

