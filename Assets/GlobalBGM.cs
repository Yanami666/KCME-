using UnityEngine;

public class GlobalBGM : MonoBehaviour
{
    public AudioClip bgmClip;
    public float volume = 0.5f;

    private static GlobalBGM instance;
    private AudioSource audioSource;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject); // 防止重复创建
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = bgmClip;
        audioSource.loop = true;
        audioSource.volume = volume;
        audioSource.Play();
    }
}