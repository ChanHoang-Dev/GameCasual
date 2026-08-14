using UnityEngine;

public class GameplayAudioTrigger : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        AudioManager.Instance?.PlayMusicFor(MusicType.GamePlay, true);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
