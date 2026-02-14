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

    private Vector2 CalculateHopDirection()
    {
        Vector2 playerPosition = (Vector2)m_player.transform.position;
        Vector2 targetPosition = (Vector2)transform.position;

        Vector2 vectorBetween = targetPosition - playerPosition;
        Vector2 directionToHop = vectorBetween.normalized;

        return directionToHop;
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