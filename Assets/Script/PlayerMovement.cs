using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    public enum Facing { Down, Up, Left, Right }

    [Header("Move (Top-Down 2D)")]
    public float moveSpeed = 4.5f;
    public bool normalizeDiagonal = true;

    [Header("Renderer")]
    public SpriteRenderer bodyRenderer;

    // ---------------- IDLE (2 frames) ----------------
    [Header("Idle (2 frames breathing)")]
    public Sprite[] idleFrames;          // size 2 recommended
    public float idleFps = 2.0f;         // breathing speed
    public float idleDelay = 0.15f;      // stop moving -> wait -> start idle anim

    // ---------------- WALK ----------------
    [Header("Walk Frames (Directional)")]
    public Sprite[] walkRight; // left uses flipX
    public Sprite[] walkUp;
    public Sprite[] walkDown;
    public float walkFps = 12f;

    // ---------------- AUDIO ----------------
    [Header("Audio Sources")]
    public AudioSource moveSource; // loop footsteps
    public AudioSource sfxSource;  // one-shots

    [Header("Move Sound")]
    public AudioClip moveLoopClip;
    [Range(0f, 1f)] public float moveVolume = 0.7f;

    [Header("Action Sound Clips")]
    public AudioClip peeClip;
    public AudioClip fartClip;
    public AudioClip danceClip;
    [Range(0f, 1f)] public float actionSfxVolume = 1.0f;

    // ---------------- PEE (J) ----------------
    [Header("Pee (J) Body Animation")]
    public Sprite[] peeFrames;
    public float peeFps = 10f;
    public float peeLockSeconds = 1.0f;   // 建议 >= peeSpawnDelay

    [Header("Pee (J) Spawn")]
    public GameObject peePrefab;
    public Transform peeSpawnPoint; // mirrored with facing
    public Vector3 peeFallbackWorldOffset = new Vector3(0f, -0.15f, 0f);

    [Header("Pee Spawn Timing")]
    public float peeSpawnDelay = 1.0f; // ✅ 按下J后延迟生成尿prefab（秒）

    // ---------------- FART (K) ----------------
    [Header("Fart (K) Body Animation (no spawn, no point)")]
    public Sprite[] fartFrames;
    public float fartFps = 12f;
    public float fartLockSeconds = 0.35f;
    public bool lockMovementWhileFart = false;

    // ---------------- DANCE (L) ----------------
    [Header("Dance (L) Body Animation")]
    public Sprite[] danceFrames;
    public float danceFps = 12f;
    public bool lockMovementWhileDancing = true;

    // =============== runtime ===============
    private Rigidbody2D rb;
    private Vector2 input;
    private bool isMoving;

    private Facing lastFacing = Facing.Down;

    // global anim state
    private enum BodyState { Normal, Pee, Fart, Dance }
    private BodyState bodyState = BodyState.Normal;

    private float stateTimer;         // for timed states (pee/fart)
    private float frameTimer;         // generic frame timer
    private int frameIndex;

    // idle timer
    private float idleWaitTimer;
    private float idleFrameTimer;
    private int idleFrameIndex;

    // pee spawn mirroring + delayed spawn routine
    private Vector3 peeSpawnLocalBase;
    private Coroutine peeSpawnRoutine;

    public bool IsMoving => isMoving;
    public Facing CurrentFacing => lastFacing;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        if (bodyRenderer == null)
            bodyRenderer = GetComponentInChildren<SpriteRenderer>();

        // AudioSource auto setup
        if (moveSource == null)
        {
            moveSource = GetComponent<AudioSource>();
            if (moveSource == null) moveSource = gameObject.AddComponent<AudioSource>();
        }
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        moveSource.playOnAwake = false;
        moveSource.loop = true;
        moveSource.volume = moveVolume;
        moveSource.clip = moveLoopClip;

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.volume = actionSfxVolume;

        if (peeSpawnPoint != null)
            peeSpawnLocalBase = peeSpawnPoint.localPosition;

        ApplyFacingMirror(lastFacing);
        ShowIdleFrame0();
    }

    void Update()
    {
        UpdateBodyStateTimers();
        ReadKeyboardMoveInput();

        isMoving = input.sqrMagnitude > 0.0001f;

        // Update facing only in Normal state while moving
        if (bodyState == BodyState.Normal && isMoving)
            lastFacing = GetFacingFromInput(input);

        ApplyFacingMirror(lastFacing);
        HandleMoveSound();

        // Render priority: Pee/Fart/Dance > Walk > Idle
        if (bodyState == BodyState.Pee)
        {
            PlayBodyFrames(peeFrames, peeFps);
            return;
        }
        if (bodyState == BodyState.Fart)
        {
            PlayBodyFrames(fartFrames, fartFps);
            return;
        }
        if (bodyState == BodyState.Dance)
        {
            PlayBodyFrames(danceFrames, danceFps);
            return;
        }

        // Normal: walk or idle
        if (isMoving)
        {
            idleWaitTimer = 0f;
            PlayWalkByFacing(lastFacing);
        }
        else
        {
            idleWaitTimer += Time.deltaTime;
            if (idleWaitTimer >= idleDelay)
                PlayIdleTwoFrames();
            else
                ShowIdleFrame0();
        }
    }

    void FixedUpdate()
    {
        // hard lock for pee
        if (bodyState == BodyState.Pee)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (bodyState == BodyState.Dance && lockMovementWhileDancing)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (bodyState == BodyState.Fart && lockMovementWhileFart)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = input * moveSpeed;
    }

    // =========================
    // INPUT
    // =========================
    private void ReadKeyboardMoveInput()
    {
        // block movement input during certain states if desired
        if (bodyState == BodyState.Pee)
        {
            input = Vector2.zero;
            return;
        }

        if (bodyState == BodyState.Dance && lockMovementWhileDancing)
        {
            input = Vector2.zero;
            return;
        }

        if (bodyState == BodyState.Fart && lockMovementWhileFart)
        {
            input = Vector2.zero;
            return;
        }

        var k = Keyboard.current;
        if (k == null)
        {
            input = Vector2.zero;
            return;
        }

        float x = 0f, y = 0f;
        if (k.aKey.isPressed || k.leftArrowKey.isPressed) x -= 1f;
        if (k.dKey.isPressed || k.rightArrowKey.isPressed) x += 1f;
        if (k.sKey.isPressed || k.downArrowKey.isPressed) y -= 1f;
        if (k.wKey.isPressed || k.upArrowKey.isPressed) y += 1f;

        input = new Vector2(x, y);

        if (normalizeDiagonal && input.sqrMagnitude > 1f)
            input = input.normalized;
    }

    private Facing GetFacingFromInput(Vector2 v)
    {
        if (Mathf.Abs(v.x) > Mathf.Abs(v.y))
            return v.x >= 0 ? Facing.Right : Facing.Left;
        else
            return v.y >= 0 ? Facing.Up : Facing.Down;
    }

    // =========================
    // FACING MIRROR
    // =========================
    private void ApplyFacingMirror(Facing f)
    {
        if (bodyRenderer == null) return;

        bool left = (f == Facing.Left);
        bodyRenderer.flipX = left; // right original, left mirrored

        // mirror pee spawn local X
        if (peeSpawnPoint != null)
        {
            Vector3 p = peeSpawnLocalBase;
            p.x = left ? -Mathf.Abs(p.x) : Mathf.Abs(p.x);
            peeSpawnPoint.localPosition = p;
        }
    }

    // =========================
    // AUDIO
    // =========================
    private void HandleMoveSound()
    {
        if (moveLoopClip == null) return;

        bool blocked = (bodyState == BodyState.Pee)
                       || (bodyState == BodyState.Dance && lockMovementWhileDancing)
                       || (bodyState == BodyState.Fart && lockMovementWhileFart);

        bool shouldPlay = isMoving && !blocked;

        if (shouldPlay)
        {
            if (!moveSource.isPlaying) moveSource.Play();
        }
        else
        {
            if (moveSource.isPlaying) moveSource.Stop();
        }
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, actionSfxVolume);
    }

    // =========================
    // ANIMATION HELPERS
    // =========================
    private void PlayWalkByFacing(Facing f)
    {
        Sprite[] frames;

        if (f == Facing.Right || f == Facing.Left) frames = walkRight;
        else if (f == Facing.Up) frames = walkUp;
        else frames = walkDown;

        PlayBodyFrames(frames, walkFps);
    }

    private void PlayBodyFrames(Sprite[] frames, float fps)
    {
        if (bodyRenderer == null) return;
        if (frames == null || frames.Length == 0) return;

        if (frames.Length == 1 || fps <= 0.01f)
        {
            bodyRenderer.sprite = frames[0];
            return;
        }

        frameTimer += Time.deltaTime;
        float dt = 1f / Mathf.Max(1f, fps);

        if (frameTimer >= dt)
        {
            frameTimer -= dt;
            frameIndex = (frameIndex + 1) % frames.Length;
            if (frames[frameIndex] != null)
                bodyRenderer.sprite = frames[frameIndex];
        }
    }

    private void ShowIdleFrame0()
    {
        if (bodyRenderer == null) return;
        if (idleFrames == null || idleFrames.Length == 0) return;

        bodyRenderer.sprite = idleFrames[0];
        idleFrameTimer = 0f;
        idleFrameIndex = 0;
    }

    private void PlayIdleTwoFrames()
    {
        if (bodyRenderer == null) return;
        if (idleFrames == null || idleFrames.Length == 0) return;

        // if only 1 provided, just show it
        if (idleFrames.Length == 1 || idleFps <= 0.01f)
        {
            bodyRenderer.sprite = idleFrames[0];
            return;
        }

        idleFrameTimer += Time.deltaTime;
        float dt = 1f / Mathf.Max(1f, idleFps);

        if (idleFrameTimer >= dt)
        {
            idleFrameTimer -= dt;
            idleFrameIndex = (idleFrameIndex + 1) % Mathf.Min(2, idleFrames.Length);
            bodyRenderer.sprite = idleFrames[idleFrameIndex];
        }
    }

    // =========================
    // BODY STATE TIMERS
    // =========================
    private void UpdateBodyStateTimers()
    {
        if (bodyState == BodyState.Pee || bodyState == BodyState.Fart)
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                bodyState = BodyState.Normal;
                frameTimer = 0f;
                frameIndex = 0;
            }
        }
        // Dance state controlled externally
    }

    // =========================================================
    // PUBLIC API (called by PlayerKeyActions / game logic)
    // =========================================================

    /// <summary>
    /// J pee success:
    /// - plays pee body animation immediately
    /// - spawns pee prefab AFTER peeSpawnDelay seconds
    /// - plays pee sound immediately
    /// </summary>
    public void PlayPeeFeedback()
    {
        PlayOneShot(peeClip);

        // start pee body animation immediately
        if (peeFrames != null && peeFrames.Length > 0)
        {
            bodyState = BodyState.Pee;
            stateTimer = Mathf.Max(0.1f, peeLockSeconds);
            frameTimer = 0f;
            frameIndex = 0;
        }

        // cancel previous delayed spawn if any
        if (peeSpawnRoutine != null)
        {
            StopCoroutine(peeSpawnRoutine);
            peeSpawnRoutine = null;
        }

        peeSpawnRoutine = StartCoroutine(SpawnPeeAfterDelay());
    }

    private System.Collections.IEnumerator SpawnPeeAfterDelay()
    {
        float t = Mathf.Max(0f, peeSpawnDelay);
        if (t > 0f)
            yield return new WaitForSeconds(t);

        if (peePrefab != null)
        {
            Vector3 pos = (peeSpawnPoint != null)
                ? peeSpawnPoint.position
                : transform.position + peeFallbackWorldOffset;

            Instantiate(peePrefab, pos, Quaternion.identity);
        }

        peeSpawnRoutine = null;
    }

    /// <summary>
    /// K fart success:
    /// - plays fart body frames (no spawn, no point)
    /// - plays fart sound
    /// </summary>
    public void PlayFartFeedback()
    {
        PlayOneShot(fartClip);

        if (fartFrames == null || fartFrames.Length == 0) return;

        bodyState = BodyState.Fart;
        stateTimer = Mathf.Max(0.1f, fartLockSeconds);
        frameTimer = 0f;
        frameIndex = 0;
    }

    /// <summary>
    /// L dance on/off (called by PlayerKeyActions L hold).
    /// </summary>
    public void SetDanceFeedbackActive(bool active)
    {
        if (active)
        {
            if (danceFrames == null || danceFrames.Length == 0) return;

            if (bodyState != BodyState.Dance)
            {
                bodyState = BodyState.Dance;
                frameTimer = 0f;
                frameIndex = 0;
                PlayOneShot(danceClip);
            }
        }
        else
        {
            if (bodyState == BodyState.Dance)
            {
                bodyState = BodyState.Normal;
                frameTimer = 0f;
                frameIndex = 0;
            }
        }
    }
}