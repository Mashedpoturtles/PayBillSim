using UnityEngine;
using MovementEffects;
using System.Collections.Generic;
using Pathfinding;
using Pathfinding.RVO;

[RequireComponent ( typeof ( Seeker ) )]
[AddComponentMenu ( "Pathfinding/AI/AIPath (3D)" )]
[HelpURL ( "http://arongranberg.com/astar/docs/class_a_i_path.php" )]
public class AIPath : MonoBehaviour
    {

    public float repathRate = 0.1F;

    public Animator anim;
    public Transform target;

    /** Enables or disables searching for paths.
	 * Setting this to false does not stop any active path requests from being calculated or stop it from continuing to follow the current path.
	 * \see #canMove
	 */
    public bool canSearch = true;

    /** Enables or disables movement.
	 * \see #canSearch */
    public bool canMove = true;

    /** Maximum velocity.
	 * This is the maximum speed in world units per second.
	 */
    public float speed = 3;

    /** Rotation speed.
	 * Rotation is calculated using Quaternion.SLerp. This variable represents the damping, the higher, the faster it will be able to rotate.
	 */
    public float turningSpeed = 5;

    /** Distance from the target point where the AI will start to slow down.
	 * Note that this doesn't only affect the end point of the path
	 * but also any intermediate points, so be sure to set #forwardLook and #pickNextWaypointDist to a higher value than this
	 */
    public float slowdownDistance = 0.6F;

    /** Determines within what range it will switch to target the next waypoint in the path */
    public float pickNextWaypointDist = 2;

    /** Target point is Interpolated on the current segment in the path so that it has a distance of #forwardLook from the AI.
	 * See the detailed description of AIPath for an illustrative image */
    public float forwardLook = 1;

    /** Distance to the end point to consider the end of path to be reached.
	 * When this has been reached, the AI will not move anymore until the target changes and OnTargetReached will be called.
	 */
    public float endReachedDistance = 0.2F;

    /** Do a closest point on path check when receiving path callback.
	 * Usually the AI has moved a bit between requesting the path, and getting it back, and there is usually a small gap between the AI
	 * and the closest node.
	 * If this option is enabled, it will simulate, when the path callback is received, movement between the closest node and the current
	 * AI position. This helps to reduce the moments when the AI just get a new path back, and thinks it ought to move backwards to the start of the new path
	 * even though it really should just proceed forward.
	 */
    public bool closestOnPathCheck = true;

    protected float minMoveScale = 0.05F;

    /** Cached Seeker component */
    protected Seeker seeker;

    /** Cached Transform component */
    protected Transform tr;

    /** Time when the last path request was sent */
    protected float lastRepath = -9999;

    /** Current path which is followed */
    protected Path path;

    /** Cached CharacterController component */
    protected CharacterController controller;


    /** Cached Rigidbody component */
    protected Rigidbody rigid;

    /** Current index in the path which is current target */
    protected int currentWaypointIndex = 0;

    /** Holds if the end-of-path is reached
	 * \see TargetReached */
    [SerializeField]
    protected bool targetReached = false;

    /** Only when the previous path has been returned should be search for a new path */
    protected bool canSearchAgain = true;

    protected Vector3 lastFoundWaypointPosition;
    protected float lastFoundWaypointTime = -9999;

    /** Returns if the end-of-path has been reached
	 * \see targetReached */
    public bool TargetReached
        {
        get
            {
            return targetReached;
            }
        }

    /** Holds if the Start function has been run.
	 * Used to test if coroutines should be started in OnEnable to prevent calculating paths
	 * in the awake stage (or rather before start on frame 0).
	 */
    private bool startHasRun = false;

    /** Initializes reference variables.
	 * If you override this function you should in most cases call base.Awake () at the start of it.
	 * */
    protected virtual void Awake ( )
        {
        seeker = GetComponent<Seeker> ( );

        //This is a simple optimization, cache the transform component lookup
        tr = transform;

        //Cache some other components (not all are necessarily there)
        controller = GetComponent<CharacterController> ( );
        rigid = GetComponent<Rigidbody> ( );
        anim = GetComponent<Animator> ( );
        }

    public void StartMoving ( )
        {
        lastRepath = -9999;
        canSearchAgain = true;
        lastFoundWaypointPosition = GetFeetPosition ( );

        //Make sure we receive callbacks when paths complete
        seeker.pathCallback += OnPathComplete;
        Timing.RunCoroutine ( _RepeatTrySearchPath ( ) );

        }

    public void OnDisable ( )
        {
        // Abort calculation of path
        if ( seeker != null && !seeker.IsDone ( ) ) seeker.GetCurrentPath ( ).Error ( );

        // Release current path
        if ( path != null ) path.Release ( this );
        path = null;

        //Make sure we receive callbacks when paths complete
        seeker.pathCallback -= OnPathComplete;
        }

    /** Tries to search for a path every #repathRate seconds.
	 * \see TrySearchPath
	 */
    public IEnumerator<float> _RepeatTrySearchPath ( )
        {
        while ( true )
            {
            float v = TrySearchPath ( );
            yield return Timing.WaitForSeconds ( v );
            }
        }

    /** Tries to search for a path.
	 * Will search for a new path if there was a sufficient time since the last repath and both
	 * #canSearchAgain and #canSearch are true and there is a target.
	 *
	 * \returns The time to wait until calling this function again (based on #repathRate)
	 */
    public float TrySearchPath ( )
        {
        if ( Time.time - lastRepath >= repathRate && canSearchAgain && canSearch && target != null )
            {
            SearchPath ( );
            return repathRate;
            }
        else {
            //StartCoroutine (WaitForRepath ());
            float v = repathRate - ( Time.time - lastRepath );
            return v < 0 ? 0 : v;
            }
        }

    /** Requests a path to the target */
    public virtual void SearchPath ( )
        {
        if ( target == null ) throw new System.InvalidOperationException ( "Target is null" );

        lastRepath = Time.time;
        //This is where we should search to
        Vector3 targetPosition = target.position;

        canSearchAgain = false;

        //Alternative way of requesting the path
        //ABPath p = ABPath.Construct (GetFeetPosition(),targetPoint,null);
        //seeker.StartPath (p);

        //We should search from the current position
        seeker.StartPath ( GetFeetPosition ( ), targetPosition );
        }

    public virtual void OnTargetReached ( )
        {
        //canMove = false;
        rigid.velocity = Vector3.zero;
        }

    /** Called when a requested path has finished calculation.
	 * A path is first requested by #SearchPath, it is then calculated, probably in the same or the next frame.
	 * Finally it is returned to the seeker which forwards it to this function.\n
	 */
    public virtual void OnPathComplete ( Path _p )
        {
        ABPath p = _p as ABPath;

        if ( p == null ) throw new System.Exception ( "This function only handles ABPaths, do not use special path types" );

        canSearchAgain = true;

        //Claim the new path
        p.Claim ( this );

        // Path couldn't be calculated of some reason.
        // More info in p.errorLog (debug string)
        if ( p.error )
            {
            p.Release ( this );
            return;
            }

        //Release the previous path
        if ( path != null ) path.Release ( this );

        //Replace the old path
        path = p;

        //Reset some variables
        currentWaypointIndex = 0;
        targetReached = false;

        //The next row can be used to find out if the path could be found or not
        //If it couldn't (error == true), then a message has probably been logged to the console
        //however it can also be got using p.errorLog
        //if (p.error)

        if ( closestOnPathCheck )
            {
            // Simulate movement from the point where the path was requested
            // to where we are right now. This reduces the risk that the agent
            // gets confused because the first point in the path is far away
            // from the current position (possibly behind it which could cause
            // the agent to turn around, and that looks pretty bad).
            Vector3 p1 = Time.time - lastFoundWaypointTime < 0.3f ? lastFoundWaypointPosition : p.originalStartPoint;
            Vector3 p2 = GetFeetPosition ( );
            Vector3 dir = p2 - p1;
            float magn = dir.magnitude;
            dir /= magn;
            int steps = ( int ) ( magn / pickNextWaypointDist );


            for ( int i = 0 ; i <= steps ; i++ )
                {
                CalculateVelocity ( p1 );
                p1 += dir;
                }
            }
        }

    public virtual Vector3 GetFeetPosition ( )
        {
        if ( controller != null )
            {
            return tr.position - Vector3.up * controller.height * 0.5F;
            }

        return tr.position;
        }

    public virtual void Update ( )
        {
        if ( !canMove ) { return; }

        Vector3 dir = CalculateVelocity ( GetFeetPosition ( ) );

        //Rotate towards targetDirection (filled in by CalculateVelocity)
        RotateTowards ( targetDirection );

        if ( controller != null )
            {
            controller.SimpleMove ( dir );
            }
        else if ( rigid != null )
            {
            rigid.AddForce ( dir );
            anim.SetFloat ( "Speed", rigid.velocity.magnitude );
            }
        else {
            tr.Translate ( dir * Time.deltaTime, Space.World );
            }
        }

    /** Point to where the AI is heading.
	 * Filled in by #CalculateVelocity */
    protected Vector3 targetPoint;
    /** Relative direction to where the AI is heading.
	 * Filled in by #CalculateVelocity */
    protected Vector3 targetDirection;

    protected float XZSqrMagnitude ( Vector3 a, Vector3 b )
        {
        float dx = b.x - a.x;
        float dz = b.z - a.z;

        return dx * dx + dz * dz;
        }

    /** Calculates desired velocity.
	 * Finds the target path segment and returns the forward direction, scaled with speed.
	 * A whole bunch of restrictions on the velocity is applied to make sure it doesn't overshoot, does not look too far ahead,
	 * and slows down when close to the target.
	 * /see speed
	 * /see endReachedDistance
	 * /see slowdownDistance
	 * /see CalculateTargetPoint
	 * /see targetPoint
	 * /see targetDirection
	 * /see currentWaypointIndex
	 */
    protected Vector3 CalculateVelocity ( Vector3 currentPosition )
        {
        if ( path == null || path.vectorPath == null || path.vectorPath.Count == 0 )
            {
            return Vector3.zero;
            }

        List<Vector3> vPath = path.vectorPath;

        if ( vPath.Count == 1 )
            {
            vPath.Insert ( 0, currentPosition );
            }

        if ( currentWaypointIndex >= vPath.Count ) { currentWaypointIndex = vPath.Count - 1; }

        if ( currentWaypointIndex <= 1 ) currentWaypointIndex = 1;

        while ( true )
            {
            if ( currentWaypointIndex < vPath.Count - 1 )
                {
                //There is a "next path segment"
                float dist = XZSqrMagnitude ( vPath [ currentWaypointIndex ], currentPosition );
                //Mathfx.DistancePointSegmentStrict (vPath[currentWaypointIndex+1],vPath[currentWaypointIndex+2],currentPosition);
                if ( dist < pickNextWaypointDist * pickNextWaypointDist )
                    {
                    lastFoundWaypointPosition = currentPosition;
                    lastFoundWaypointTime = Time.time;
                    currentWaypointIndex++;
                    }
                else {
                    break;
                    }
                }
            else {
                break;
                }
            }

        Vector3 dir = vPath [ currentWaypointIndex ] - vPath [ currentWaypointIndex - 1 ];
        Vector3 targetPosition = CalculateTargetPoint ( currentPosition, vPath [ currentWaypointIndex - 1 ], vPath [ currentWaypointIndex ] );


        dir = targetPosition - currentPosition;
        dir.y = 0;
        float targetDist = dir.magnitude;

        float slowdown = Mathf.Clamp01 ( targetDist / slowdownDistance );

        this.targetDirection = dir;
        this.targetPoint = targetPosition;

        if ( currentWaypointIndex == vPath.Count - 1 && targetDist <= endReachedDistance )
            {
            if ( !targetReached ) { targetReached = true; OnTargetReached ( ); }

            //Send a move request, this ensures gravity is applied
            return Vector3.zero;
            }

        Vector3 forward = tr.forward;
        float dot = Vector3.Dot ( dir.normalized, forward );
        float sp = speed * Mathf.Max ( dot, minMoveScale ) * slowdown;


        if ( Time.deltaTime > 0 )
            {
            sp = Mathf.Clamp ( sp, 0, targetDist / ( Time.deltaTime * 2 ) );
            }
        return forward * sp;
        }

    /** Rotates in the specified direction.
	 * Rotates around the Y-axis.
	 * \see turningSpeed
	 */
    protected virtual void RotateTowards ( Vector3 dir )
        {
        if ( dir == Vector3.zero ) return;

        Quaternion rot = tr.rotation;
        Quaternion toTarget = Quaternion.LookRotation ( dir );

        rot = Quaternion.Slerp ( rot, toTarget, turningSpeed * Time.deltaTime );
        Vector3 euler = rot.eulerAngles;
        euler.z = 0;
        euler.x = 0;
        rot = Quaternion.Euler ( euler );

        tr.rotation = rot;
        }

    /** Calculates target point from the current line segment.
	 * \param p Current position
	 * \param a Line segment start
	 * \param b Line segment end
	 * The returned point will lie somewhere on the line segment.
	 * \see #forwardLook
	 * \todo This function uses .magnitude quite a lot, can it be optimized?
	 */
    protected Vector3 CalculateTargetPoint ( Vector3 p, Vector3 a, Vector3 b )
        {
        a.y = p.y;
        b.y = p.y;

        float magn = ( a - b ).magnitude;
        if ( magn == 0 ) return a;

        float closest = Mathf.Clamp01 ( VectorMath.ClosestPointOnLineFactor ( a, b, p ) );
        Vector3 point = ( b - a ) * closest + a;
        float distance = ( point - p ).magnitude;

        float lookAhead = Mathf.Clamp ( forwardLook - distance, 0.0F, forwardLook );

        float offset = lookAhead / magn;
        offset = Mathf.Clamp ( offset + closest, 0.0F, 1.0F );
        return ( b - a ) * offset + a;
        }
    }