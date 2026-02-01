using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class ArrestManager : MonoBehaviour
{
    [Header("UI (optional)")]
    public GameObject arrestedPanel;
    public TextMeshProUGUI arrestedText;

    [Header("Freeze")]
    public bool pauseTimeOnArrest = true;

    [Header("Sound")]
    public AudioSource sfxSource;
    public AudioClip arrestClip;
    [Range(0f, 1f)] public float arrestVolume = 1f;

    private bool arrested;
    public bool IsArrested => arrested;

    void Awake()
    {
        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();
    }

    public void Arrest(string reason)
    {
        if (arrested) return;
        arrested = true;

        if (sfxSource != null && arrestClip != null)
            sfxSource.PlayOneShot(arrestClip, arrestVolume);

        if (pauseTimeOnArrest)
            Time.timeScale = 0f;

        if (arrestedPanel != null)
            arrestedPanel.SetActive(true);

        if (arrestedText != null)
            arrestedText.text = $"BUSTED!\n{reason}";
    }

    void Update()
    {
        if (!arrested) return;

        // debug reset
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            Time.timeScale = 1f;
            arrested = false;
            if (arrestedPanel != null) arrestedPanel.SetActive(false);
        }
    }
}