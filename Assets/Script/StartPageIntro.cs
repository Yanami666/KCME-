using UnityEngine;

public class StartPageIntro : MonoBehaviour
{
    [Header("UI")]
    public GameObject introPanel;     // Canvas下的那张图片Panel

    [Header("Timing")]
    public float showSeconds = 10f;   // 显示多久（10秒）

    [Header("Audio")]
    public AudioSource sfxSource;     // 可选：同物体AudioSource
    public AudioClip introClip;
    [Range(0f, 1f)] public float volume = 1f;

    [Header("Optional: Lock gameplay during intro")]
    public bool lockGameplay = false;
    public Behaviour[] disableThese; // 可拖 PlayerMovement / PlayerKeyActions / NPC脚本等

    void Awake()
    {
        // 防止上一次 Arrest 把 timeScale=0 导致计时不走
        Time.timeScale = 1f;

        if (introPanel != null)
            introPanel.SetActive(true);

        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();

        if (lockGameplay && disableThese != null)
        {
            for (int i = 0; i < disableThese.Length; i++)
                if (disableThese[i] != null) disableThese[i].enabled = false;
        }
    }

    void Start()
    {
        if (sfxSource != null && introClip != null)
            sfxSource.PlayOneShot(introClip, volume);

        Invoke(nameof(HideIntro), Mathf.Max(0.05f, showSeconds));
    }

    void HideIntro()
    {
        if (introPanel != null)
            introPanel.SetActive(false);

        if (lockGameplay && disableThese != null)
        {
            for (int i = 0; i < disableThese.Length; i++)
                if (disableThese[i] != null) disableThese[i].enabled = true;
        }
    }
}