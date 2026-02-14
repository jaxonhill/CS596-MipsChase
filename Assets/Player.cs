using UnityEngine;

public class Player : MonoBehaviour
{
    // External tunables.
    static public float m_fMaxSpeed = 0.10f;
    public float m_fSlowSpeed = m_fMaxSpeed * 0.66f;
    public float m_fIncSpeed = 0.0025f;
    public float m_fMagnitudeFast = 0.6f;
    public float m_fMagnitudeSlow = 0.06f;
    public float m_fFastRotateSpeed = 0.2f;
    public float m_fFastRotateMax = 10.0f;
    public float m_fDiveTime = 0.3f;
    public float m_fDiveRecoveryTime = 0.5f;
    public float m_fDiveDistance = 3.0f;

    // Internal variables.
    public eState m_nState;
    public Vector3 m_vDiveStartPos;
    public Vector3 m_vDiveEndPos;
    public float m_fDiveStartTime;
    public float m_fDiveRecoveryStartTime;
    public float m_fTargetSpeed;
    public float m_fTargetAngle;
    public float m_fAngle;
    public float m_fSpeed;

    public enum eState : int
    {
        kMoveSlow,
        kMoveFast,
        kDiving,
        kRecovering,
        kNumStates
    }

    private Color[] stateColors = new Color[(int)eState.kNumStates]
    {
        new(0,     0,   0),
        new(255, 255, 255),
        new(0,     0, 255),
        new(0,   255,   0),
    };

    // -- STATE: Diving --

    public bool IsDiving()
    {
        return m_nState == eState.kDiving;
    }

    private void Dive()
    {
        // TODO: Make it actually move the player over an elapsed time toward target position

        // Action: Move in direction of dive, attach rabbit if within reach, ignore any other input.
        // Linear movement from a -> b
        // Given time and distance -> determine velocity (speed)
        // Simplest version is to just teleport the player to the end position

        transform.position = m_vDiveEndPos;
    } 

    private bool DoesWantToDive()
    {
        return Input.GetMouseButton(0);
    }

    private void StartDive()
    {
        m_nState = eState.kDiving; 
        m_fSpeed = 0.0f;
        m_vDiveStartPos = transform.position;
        m_vDiveEndPos = m_vDiveStartPos - (transform.right * m_fDiveDistance);
        m_fDiveStartTime = Time.time;
    }

    private bool IsDiveTimeLimitReached()
    {
        float currentTime = Time.time;
        float elapsedTime = currentTime - m_fDiveStartTime;
        return elapsedTime >= m_fDiveTime;
    }

    // -- STATE: Recovering -- 
    
    private void StartRecovering()
    {
        m_nState = eState.kRecovering;
        m_fSpeed = 0.0f;
        m_fDiveRecoveryStartTime = Time.time;
    }

    private bool IsRecoveringTimeLimitReached()
    {
        float currentTime = Time.time;
        float elapsedTime = currentTime - m_fDiveRecoveryStartTime;
        return elapsedTime >= m_fDiveRecoveryTime;
    }

    void Start()
    {
        m_fAngle = 0;
        m_fSpeed = 0;
        m_nState = eState.kMoveSlow;
    }

    void FixedUpdate()
    {
        switch (m_nState)
        {
            case eState.kMoveSlow:
                // Move Slow:
                    // Rotation angle can change immediately
                    // Move with slow speed

                if (m_fSpeed > m_fSlowSpeed) { m_nState = eState.kMoveFast; };
                if (DoesWantToDive()) { StartDive(); };
                break;
            case eState.kMoveFast:
                // Move Fast:
                    // Rotation cannot exceed a small threshold
                    // If rotation thresh. is met, player continues in original dir., but starts slowing down
                if (m_fSpeed < m_fSlowSpeed) { m_nState = eState.kMoveSlow; };
                if (DoesWantToDive()) { StartDive(); };
                break;
            case eState.kDiving:
                Dive();
                if (IsDiveTimeLimitReached()) { StartRecovering(); };
                break;
            case eState.kRecovering:
                if (IsRecoveringTimeLimitReached()) { m_nState = eState.kMoveSlow; }
                break;
            default:
                break;
        }

        // Actions to perform regardless of state
        GetComponent<Renderer>().material.color = stateColors[(int)m_nState];
        UpdateDirectionAndSpeed();
    }

    // -- Functions that probably need to change later --

    // Given this function as starter
    void UpdateDirectionAndSpeed()
    {
        // Get relative positions between the mouse and player
        Vector3 vScreenPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 vScreenSize = Camera.main.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height));
        Vector2 vOffset = new Vector2(transform.position.x - vScreenPos.x, transform.position.y - vScreenPos.y);

        // Find the target angle being requested.
        m_fTargetAngle = Mathf.Atan2(vOffset.y, vOffset.x) * Mathf.Rad2Deg;

        // Calculate how far away from the player the mouse is.
        float fMouseMagnitude = vOffset.magnitude / vScreenSize.magnitude;

        // Based on distance, calculate the speed the player is requesting.
        if (fMouseMagnitude > m_fMagnitudeFast)
        {
            m_fTargetSpeed = m_fMaxSpeed;
        }
        else if (fMouseMagnitude > m_fMagnitudeSlow)
        {
            m_fTargetSpeed = m_fSlowSpeed;
        }
        else
        {
            m_fTargetSpeed = 0.0f;
        }
    }

    // Prev. simple rotate code without states
    void RotatePlayer()
    {
        transform.rotation = Quaternion.Euler(0, 0, m_fTargetAngle);
    }

    // Prev. simple move code without states
    void Move()
    {
        transform.position += -1 * m_fTargetSpeed * transform.right;
    }

}
