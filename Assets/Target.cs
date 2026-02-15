using UnityEngine;

public class Target : MonoBehaviour
{
    public Player m_player;
    public enum eState : int
    {
        kIdle,
        kHopStart,
        kHop,
        kCaught,
        kNumStates
    }

    private Color[] stateColors = new Color[(int)eState.kNumStates]
    {
        new Color(255, 0,   0),
        new Color(0,   255, 0),
        new Color(0,   0,   255),
        new Color(255, 255, 255)
    };

    // External tunables.
    public float m_fHopTime = 0.2f;
    public float m_fHopSpeed = 6.5f;
    public float m_fScaredDistance = 3.0f;
    public int m_nMaxMoveAttempts = 50;
    public int boundsMargin = 1;

    // Internal variables.
    public eState m_nState;
    public float m_fHopStartTime;
    public Vector2 m_fHopDirection; 

    private bool IsInScaredDistance()
    {
        Vector2 playerPosition = (Vector2)m_player.transform.position;
        float distanceFromPlayer = Vector2.Distance((Vector2)transform.position, playerPosition);
        return distanceFromPlayer < m_fScaredDistance;
    }

    private bool WillHopBeInBounds(Vector2 hopDirection)
    {
        Vector2 currentPosition = (Vector2)transform.position;
        Vector2 displacement = m_fHopSpeed * m_fHopTime * hopDirection;
        Vector2 endPosition = currentPosition + displacement; 

        Vector2 min = Camera.main.ScreenToWorldPoint(new Vector2(0, 0));
        Vector2 max = Camera.main.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height));

        // Account for margin on edges of screen so we can see the rabbit
        min += Vector2.one * boundsMargin;
        max -= Vector2.one * boundsMargin;

        return endPosition.x >= min.x && endPosition.x <= max.x && endPosition.y >= min.y && endPosition.y <= max.y;
    }

    private Vector2 CalculateHopDirection()
    {
        Vector2 playerPosition = (Vector2)m_player.transform.position;
        Vector2 targetPosition = (Vector2)transform.position;
        Vector2 vectorBetween = targetPosition - playerPosition;

        // away >> left >> right >> towards
        Vector2 awayFromPlayer = vectorBetween.normalized;
        Vector2 left  = new Vector2(-awayFromPlayer.y,  awayFromPlayer.x);
        Vector2 right = new Vector2(awayFromPlayer.y, -awayFromPlayer.x);
        Vector2 towardsPlayer = -1 * awayFromPlayer;
        Vector2[] directionOptions = { awayFromPlayer, left, right, towardsPlayer };

        foreach (Vector2 dir in directionOptions)
        {
            if (WillHopBeInBounds(dir)) { return dir; }
        }

        return Vector2.zero; // edge case that should never happen, but don't move if nothing is valid.
    }

    private bool IsHopTimeLimitReached()
    {
        float currentTime = Time.time;
        float elapsedTime = currentTime - m_fHopStartTime;
        return elapsedTime >= m_fHopTime;
    }

    private void StartHop()
    {
        m_nState = eState.kHop;
        m_fHopStartTime = Time.time;
        m_fHopDirection = CalculateHopDirection();
    }

    private void Move()
    {
        transform.position += m_fHopSpeed * Time.fixedDeltaTime * (Vector3)m_fHopDirection;
    }

    void Start()
    {
        // Setup the initial state and get the player GO.
        m_nState = eState.kIdle;
        m_player = GameObject.FindObjectOfType(typeof(Player)) as Player;
    }

    void FixedUpdate()
    {
        GetComponent<Renderer>().material.color = stateColors[(int)m_nState];

        switch (m_nState)
        {
            case eState.kIdle:
                if (IsInScaredDistance()) { StartHop(); };
                break;
            case eState.kHop:
                Move();
                if (IsHopTimeLimitReached()) { m_nState = eState.kIdle; } 
                break;
            case eState.kCaught:
                break;
            default:
                break;
        } 
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        // Check if this is the player (in this situation it should be!)
        if (collision.gameObject == GameObject.Find("Player"))
        {
            // If the player is diving, it's a catch!
            if (m_player.IsDiving())
            {
                m_nState = eState.kCaught;
                transform.parent = m_player.transform;
                transform.localPosition = new Vector3(0.0f, -0.5f, 0.0f);
            }
        }
    }
}