using UnityEngine;

public class MainMenuAudioTrigger : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        AudioManager.Instance?.PlayMusicFor(MusicType.MainMenu);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
