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

    [Header("Wait")]
    public float waitSeconds = 15f;
    public Vector2 headTurnIntervalRange = new Vector2(2f, 5f);

    [Header("Sprite")]
    public SpriteRenderer spriteRenderer;
    [Tooltip("If your original sprite faces LEFT, keep this true.")]
    public bool originalFacesLeft = true;

    [Header("FOV Root (optional)")]
    public Transform fovRoot; // child object holding the FOV collider

    [Header("Idle Animation (during wait)")]
    public Sprite[] idleFrames;
    public float idleFps = 6f;

    [Header("Walk Animation (optional)")]
    public Sprite[] walkFrames;
    public float walkFps = 8f;

    private Rigidbody2D rb;

    private enum State { Moving, Waiting }
    private State state;

    private Vector2 targetPos;
    private float waitTimer;

    // anim
    private float animTimer;
    private int animIndex;

    // head turn
    private float headTurnTimer;
    private bool facingRight; // true = facing right (means flipX for original-left sprite)

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        PickNextTarget();
        state = State.Moving;
        ScheduleHeadTurn();
        ApplyFacing(false); // start facing left by default
    }

    void Update()
    {
        if (state == State.Waiting)
        {
            waitTimer -= Time.deltaTime;

            // idle anim
            PlayFrames(idleFrames, idleFps);

            // head turn occasionally
            headTurnTimer -= Time.deltaTime;
            if (headTurnTimer <= 0f)
            {
                ApplyFacing(!facingRight);
                ScheduleHeadTurn();
            }

            if (waitTimer <= 0f)
            {
                PickNextTarget();
                state = State.Moving;
                animTimer = 0f;
                animIndex = 0;
            }
        }
        else
        {
            // moving anim
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
        if (Mathf.Abs(delta.x) > 0.01f)
        {
            // left: keep original, right: flip (per your rule)
            if (delta.x > 0f) ApplyFacing(true);
            else ApplyFacing(false);
        }

        if (Vector2.Distance(next, targetPos) <= 0.05f)
        {
            state = State.Waiting;
            waitTimer = waitSeconds;
            animTimer = 0f;
            animIndex = 0;
            ScheduleHeadTurn();
        }
    }

    private void PickNextTarget()
    {
        Vector2 current = rb.position;

        Vector2 randomDir = Random.insideUnitCircle.normalized;
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

    private void ApplyFacing(bool right)
    {
        facingRight = right;

        if (spriteRenderer != null)
        {
            // if original faces LEFT:
            // left -> flipX = false, right -> flipX = true
            if (originalFacesLeft)
                spriteRenderer.flipX = facingRight;
            else
                spriteRenderer.flipX = !facingRight;
        }

        // mirror FOV
        if (fovRoot != null)
        {
            Vector3 s = fovRoot.localScale;
            s.x = Mathf.Abs(s.x) * (facingRight ? -1f : 1f);
            fovRoot.localScale = s;
        }
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
}