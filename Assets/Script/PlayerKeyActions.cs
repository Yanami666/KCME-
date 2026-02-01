using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerKeyActions : MonoBehaviour
{
    [Header("Refs")]
    public ActionExecutor executor;
    public VisibilityProbe2D playerProbe;
    public GoodHintUI goodHintUI;
    public PlayerMovement playerMovement;

    [Header("Dance (L)")]
    public float danceHoldSeconds = 1.0f;

    private bool lHeld;
    private float danceTimer;
    private bool danceRewardGiven;

    void Awake()
    {
        if (playerProbe == null) playerProbe = GetComponent<VisibilityProbe2D>();
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        var k = Keyboard.current;
        if (k == null) return;

        // L dance hold
        bool nowHeld = k.lKey.isPressed;

        if (nowHeld && !lHeld)
        {
            lHeld = true;
            danceTimer = 0f;
            danceRewardGiven = false;

            if (playerMovement != null)
                playerMovement.SetDanceFeedbackActive(true);
        }
        else if (!nowHeld && lHeld)
        {
            lHeld = false;
            danceTimer = 0f;

            if (playerMovement != null)
                playerMovement.SetDanceFeedbackActive(false);
        }

        if (lHeld)
        {
            danceTimer += Time.deltaTime;
            if (!danceRewardGiven && danceTimer >= danceHoldSeconds)
            {
                danceRewardGiven = true;
                TryGoodDance();
            }
        }

        // K fart
        if (k.kKey.wasPressedThisFrame)
        {
            if (TryFart())
            {
                if (playerMovement != null)
                    playerMovement.PlayFartFeedback();
            }
        }

        // J pee
        if (k.jKey.wasPressedThisFrame)
        {
            if (TryPee(out bool arrested))
            {
                if (playerMovement != null)
                    playerMovement.PlayPeeFeedback();
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

        if (!ok && goodHintUI != null)
            goodHintUI.Show("You need to be in NPC FOV to do this good action.");
    }

    private bool TryFart()
    {
        if (executor == null) return false;

        bool arrested;
        string reason;

        return executor.TryExecute(
            ActionExecutor.ActionType.BadPublic,
            playerProbe,
            targetProbe: null,
            customDelta: +20,
            showGoodFailHint: false,
            out reason,
            out arrested
        );
    }

    private bool TryPee(out bool arrested)
    {
        arrested = false;
        if (executor == null) return false;

        string reason;

        return executor.TryExecute(
            ActionExecutor.ActionType.BadHidden,
            playerProbe,
            targetProbe: null,
            customDelta: +10,
            showGoodFailHint: false,
            out reason,
            out arrested
        );
    }
}