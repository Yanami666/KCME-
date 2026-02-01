using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.SceneManagement;

public class ArrestManager : MonoBehaviour
{
    [Header("UI (optional)")]
    public GameObject arrestedPanel;
    public TextMeshProUGUI arrestedText;

    [Header("Freeze")]
    public bool pauseTimeOnArrest = true;

    [Header("Restart")]
    public string restartSceneName = "StartPage"; // 你要回到的Scene名字

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
            arrestedText.text = $"BUSTED!\n{reason}\n\nPress R to Restart";
    }

    void Update()
    {
        // ✅ 只有被抓后才允许按R
        if (!arrested) return;

        var k = Keyboard.current;
        if (k == null) return;

        if (k.rKey.wasPressedThisFrame)
        {
            RestartToStartPage();
        }
    }

    private void RestartToStartPage()
    {
        // ✅ 恢复时间，避免下一场景卡住
        Time.timeScale = 1f;

        // ✅ 直接切回 StartPage
        SceneManager.LoadScene(restartSceneName);
    }
}