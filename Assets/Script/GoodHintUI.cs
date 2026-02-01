using UnityEngine;
using TMPro;

public class GoodHintUI : MonoBehaviour
{
    public TextMeshProUGUI hintText;
    public float showSeconds = 1.0f;

    [Header("Sound")]
    public AudioSource sfxSource;
    public AudioClip hintClip;
    [Range(0f, 1f)] public float hintVolume = 1f;

    private float timer;

    void Awake()
    {
        if (hintText != null)
            hintText.gameObject.SetActive(false);

        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();
    }

    public void Show(string msg)
    {
        if (hintText == null) return;

        hintText.text = msg;
        hintText.gameObject.SetActive(true);
        timer = showSeconds;

        if (sfxSource != null && hintClip != null)
            sfxSource.PlayOneShot(hintClip, hintVolume);
    }

    void Update()
    {
        if (hintText == null) return;

        if (timer > 0f)
        {
            timer -= Time.unscaledDeltaTime;
            if (timer <= 0f)
                hintText.gameObject.SetActive(false);
        }
    }
}