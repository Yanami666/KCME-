using UnityEngine;
using TMPro;

public class GoodHintUI : MonoBehaviour
{
    public TextMeshProUGUI hintText;
    public float showSeconds = 1.0f;

    private float timer;

    void Awake()
    {
        if (hintText != null)
            hintText.gameObject.SetActive(false);
    }

    public void Show(string msg)
    {
        if (hintText == null) return;

        hintText.text = msg;
        hintText.gameObject.SetActive(true);
        timer = showSeconds;
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