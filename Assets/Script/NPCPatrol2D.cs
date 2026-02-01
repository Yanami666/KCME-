using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class NPCPatrol2D : MonoBehaviour
{
    [Header("Bounds (use a BoxCollider2D as patrol area)")]
    public BoxCollider2D patrolBounds;

    [Header("Move")]
    public float moveSpeed = 1.2f;
    public float minStepDistance = 3f;
    public float maxStepDistance = 7f;
    public float arriveDistance = 0.05f;

    [Header("Wait")]
    public float waitSeconds = 15f;
    public Vector2 headTurnIntervalRange = new Vector2(6f, 12f); // ✅ slower turns by default

    [Header("Mirror (IMPORTANT)")]
    [Tooltip("Put Sprite + FOV + StealZone + any colliders that must flip under this root.")]
    public Transform mirrorRoot;

    [Header("Sprite (optional: only used if mirrorRoot is null)")]
    public SpriteRenderer spriteRenderer;

    [Header("Idle Animation (during wait)")]
    public Sprite[] idleFrames;
    public float idleFps = 6f;

    [Header("Walk Animation (optional)")]
    public Sprite[] walkFrames;
    public float walkFps = 8f;

    [Header("Facing Rule")]
    [Tooltip("If true: when moving LEFT keep facingLeft; when moving RIGHT faceRight (mirror).")]
    public bool leftKeepOriginalRightFlip = true;

    private Rigidbody2D rb;

    private enum State { Moving, Waiting }
    private State state;

    private Vector2 targetPos;
    private float waitTimer;

    // animation
    private float animTimer;
    private int animIndex;

    // head turn
    private float headTurnTimer;
    private bool facingRight; // true = right, false = left

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // start facing left (original)
        SetFacingRight(false);

        PickNextTarget();
        state = State.Moving;
        ScheduleHeadTurn();
    }

    void Update()
    {
        if (state == State.Waiting)
        {
            waitTimer -= Time.deltaTime;

            // idle animation
            PlayFrames(idleFrames, idleFps);

            // head turn occasionally while waiting
            headTurnTimer -= Time.deltaTime;
            if (headTurnTimer <= 0f)
            {
                SetFacingRight(!facingRight);
                ScheduleHeadTurn();
            }

            if (waitTimer <= 0f)
            {
                PickNextTarget();
                state = State.Moving;
                ResetAnim();
            }
        }
        else
        {
            // moving animation (optional)
            if (walkFrames != null && walkFrames.Length > 0)
                PlayFrames(walkFrames, walkFps);
        }
    }

    void FixedUpdate()
    {
        if (state != State.Moving) return;

        Vector2 pos = rb.position;
        Vector2 next = Vector2.MoveTowards(pos, targetPos, moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(next);

        Vector2 delta = targetPos - pos;

        // apply facing based on horizontal movement direction
        if (Mathf.Abs(delta.x) > 0.01f)
        {
            if (leftKeepOriginalRightFlip)
            {
                // left: keep original, right: flip
                if (delta.x > 0f) SetFacingRight(true);
                else SetFacingRight(false);
            }
            else
            {
                // alternative rule: face direction of travel (left/right)
                SetFacingRight(delta.x > 0f);
            }
        }

        if (Vector2.Distance(next, targetPos) <= arriveDistance)
        {
            state = State.Waiting;
            waitTimer = waitSeconds;
            ResetAnim();
            ScheduleHeadTurn();
        }
    }

    // -----------------------
    // Core helpers
    // -----------------------

    private void PickNextTarget()
    {
        Vector2 current = rb.position;

        Vector2 randomDir = Random.insideUnitCircle;
        if (randomDir.sqrMagnitude < 0.0001f) randomDir = Vector2.right;
        randomDir = randomDir.normalized;

        float dist = Random.Range(minStepDistance, maxStepDistance);
        Vector2 candidate = current + randomDir * dist;

        candidate = ClampToBounds(candidate);
        targetPos = candidate;
    }

    private Vector2 ClampToBounds(Vector2 p)
    {
        if (patrolBounds == null) return p;

        Bounds b = patrolBounds.bounds;
        float x = Mathf.Clamp(p.x, b.min.x, b.max.x);
        float y = Mathf.Clamp(p.y, b.min.y, b.max.y);
        return new Vector2(x, y);
    }

    private void ScheduleHeadTurn()
    {
        headTurnTimer = Random.Range(headTurnIntervalRange.x, headTurnIntervalRange.y);
    }

    private void ResetAnim()
    {
        animTimer = 0f;
        animIndex = 0;
    }

    private void PlayFrames(Sprite[] frames, float fps)
    {
        if (spriteRenderer == null) return;
        if (frames == null || frames.Length == 0) return;

        if (frames.Length == 1 || fps <= 0.01f)
        {
            spriteRenderer.sprite = frames[0];
            return;
        }

        animTimer += Time.deltaTime;
        float dt = 1f / Mathf.Max(1f, fps);

        if (animTimer >= dt)
        {
            animTimer -= dt;
            animIndex = (animIndex + 1) % frames.Length;
            spriteRenderer.sprite = frames[animIndex];
        }
    }

    // -----------------------
    // Mirror (the whole NPC visuals + colliders)
    // -----------------------

    private void SetFacingRight(bool right)
    {
        facingRight = right;

        // ✅ BEST: flip mirrorRoot scale so colliders + children flip too
        if (mirrorRoot != null)
        {
            Vector3 s = mirrorRoot.localScale;
            float absX = Mathf.Abs(s.x);
            s.x = absX * (facingRight ? -1f : 1f);
            mirrorRoot.localScale = s;
            return;
        }

        // fallback (ONLY visual flip; colliders won't flip)
        if (spriteRenderer != null)
            spriteRenderer.flipX = facingRight;
    }
}