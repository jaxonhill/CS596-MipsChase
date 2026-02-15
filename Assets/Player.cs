using Unity.VisualScripting;
using UnityEngine;

public class Player : MonoBehaviour
{
    // External tunables.
    static public float m_fMaxSpeed = 5.0f;
    public float m_fFastThreshold = m_fMaxSpeed * 0.8f;
    public float m_fSlowSpeed = m_fMaxSpeed * 0.33f;
    public float m_fIncSpeed = 0.01f;
    public float m_fMagnitudeFast = 0.6f;
    public float m_fMagnitudeSlow = 0.06f;
    public float m_fFastRotateSpeed = 2.0f;
    public float m_fFastRotateMax = 15.0f;
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

    private bool DoesWantToDive()
    {
        return Input.GetMouseButton(0);
    }

    private void StartDive()
    {
        m_nState = eState.kDiving; 

        m_vDiveStartPos = transform.position;
        m_vDiveEndPos = m_vDiveStartPos - (transform.right * m_fDiveDistance);
        m_fDiveStartTime = Time.time;

        m_fSpeed = m_fDiveDistance / m_fDiveTime;
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

    // -- STATE: Move Slow -- 
    
    private void StartMoveSlow()
    {
        m_nState = eState.kMoveSlow;
        m_fSpeed = m_fSlowSpeed;
    }

    void InstantRotate()
    {
        transform.rotation = Quaternion.Euler(0, 0, m_fTargetAngle);
    }

    // -- STATE: Move Fast --

    private void StartMoveFast()
    {
        m_nState = eState.kMoveFast;
    }

    void IncrementalRotate()
    {
        float currentAngle = transform.eulerAngles.z;

        float angleDiff = Mathf.DeltaAngle(currentAngle, m_fTargetAngle);
        float absDiff = Mathf.Abs(angleDiff);
        bool isExceedingThreshold = absDiff >= m_fFastRotateMax;

        if (isExceedingThreshold)
        {
            m_fSpeed -= m_fIncSpeed;
        } 
        else
        {
            m_fSpeed += m_fIncSpeed;
        }

        // Move towards the angle regardless if we were exceeding threshold
        float finalRotation = Mathf.MoveTowardsAngle(currentAngle, m_fTargetAngle, m_fFastRotateSpeed);
        transform.rotation = Quaternion.Euler(0, 0, finalRotation);
    }

    // -- GENERAL --

    private void Move()
    {
        transform.position += -1 * m_fSpeed * Time.fixedDeltaTime * transform.right;
    }

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

    void Start()
    {
        StartMoveSlow();
    }

    void FixedUpdate()
    {
        switch (m_nState)
        {
            case eState.kMoveSlow:
                InstantRotate();
                Move();
                m_fSpeed += m_fIncSpeed;

                if (m_fSpeed > m_fFastThreshold) { StartMoveFast(); };
                if (DoesWantToDive()) { StartDive(); };
                break;
            case eState.kMoveFast:
                IncrementalRotate();
                Move();

                if (m_fSpeed < m_fFastThreshold) { StartMoveSlow(); };
                if (DoesWantToDive()) { StartDive(); };
                break;
            case eState.kDiving:
                Move();
                if (IsDiveTimeLimitReached()) { StartRecovering(); };
                break;
            case eState.kRecovering:
                if (IsRecoveringTimeLimitReached()) { StartMoveSlow(); }
                break;
            default:
                break;
        }

        // Actions to perform regardless of state
        GetComponent<Renderer>().material.color = stateColors[(int)m_nState];
        UpdateDirectionAndSpeed();
    }

}
