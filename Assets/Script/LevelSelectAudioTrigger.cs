using UnityEngine;

public class LevelSelectAudioTrigger : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        AudioManager.Instance?.PlayMusicFor(MusicType.LevelSelect);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
