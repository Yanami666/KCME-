using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerKeyActions : MonoBehaviour
{
    [Header("Refs")]
    public ActionExecutor executor;
    public VisibilityProbe2D playerProbe;
    public GoodHintUI goodHintUI;
    public PlayerMovement movementVisual;   // ✅ 用来播 J/K/L 动画

    [Header("Dance (L)")]
    public float danceHoldSeconds = 1.0f;
    private bool dancing;
    private float danceTimer;
    private bool danceRewardGiven;

    void Awake()
    {
        if (playerProbe == null) playerProbe = GetComponent<VisibilityProbe2D>();
        if (movementVisual == null) movementVisual = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        var k = Keyboard.current;
        if (k == null) return;

        // -------- L: Dance (Good) - hold >= 1s to succeed --------
        bool lHeld = k.lKey.isPressed;

        if (lHeld)
        {
            if (!dancing)
            {
                dancing = true;
                danceTimer = 0f;
                danceRewardGiven = false;

                if (movementVisual != null)
                    movementVisual.SetDanceVisualActive(true);
            }

            danceTimer += Time.deltaTime;

            if (!danceRewardGiven && danceTimer >= danceHoldSeconds)
            {
                danceRewardGiven = true;
                TryGoodDance(); // 成功/失败判定在 executor 里
            }
        }
        else
        {
            if (dancing)
            {
                dancing = false;
                danceTimer = 0f;

                if (movementVisual != null)
                    movementVisual.SetDanceVisualActive(false);
            }
        }

        // -------- K: Fart (BadPublic) --------
        if (k.kKey.wasPressedThisFrame)
        {
            if (TryFart())
            {
                if (movementVisual != null)
                    movementVisual.PlayFartVisual();
            }
        }

        // -------- J: Pee (BadHidden) --------
        if (k.jKey.wasPressedThisFrame)
        {
            if (TryPee(out bool arrested))
            {
                if (movementVisual != null)
                    movementVisual.PlayPeeVisual();
            }
        }
    }

    private void TryGoodDance()
    {
        if (executor == null) return;

        bool arrested;
        string reason;

        bool ok = executor.TryExecute(
            ActionExecutor.ActionType.Good,
            playerProbe,
            targetProbe: null,
            customDelta: -10,
            showGoodFailHint: true,
            out reason,
            out arrested
        );

        if (!ok)
        {
            if (goodHintUI != null)
                goodHintUI.Show("You need to be in NPC FOV to do this good action.");
        }
        else
        {
            Debug.Log("[Action] Dance Good Success (-10)");
        }
    }

    private bool TryFart()
    {
        if (executor == null) return false;

        bool arrested;
        string reason;

        bool ok = executor.TryExecute(
            ActionExecutor.ActionType.BadPublic,
            playerProbe,
            targetProbe: null,
            customDelta: +20,
            showGoodFailHint: false,
            out reason,
            out arrested
        );

        if (ok) Debug.Log("[Action] Fart Public Bad (+20)");
        return ok;
    }

    private bool TryPee(out bool arrested)
    {
        arrested = false;
        if (executor == null) return false;

        string reason;

        bool ok = executor.TryExecute(
            ActionExecutor.ActionType.BadHidden,
            playerProbe,
            targetProbe: null,
            customDelta: +10,
            showGoodFailHint: false,
            out reason,
            out arrested
        );

        if (ok) Debug.Log("[Action] Pee Hidden Bad (+10)");
        return ok;
    }
}