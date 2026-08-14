using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class UIMainScene : MonoBehaviour
{
    [SerializeField] private Button returnStartScene;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        returnStartScene.onClick.AddListener(OnReturnStartScene);
    }

    // Update is called once per frame
    void Update()
    {

    }
    void OnReturnStartScene()
    {
        SceneManager.LoadScene("StartScene");
    }
}
