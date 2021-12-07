using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class TestMovement : MonoBehaviour
{
    public float MAX_SPEED = 11f;
    public float MAX_ACCELERATION = 100f;
    public float MAX_AIR_ACCELERATION = 100f;
    public float friction = 5f;
    public bool speedometer = false;
    public float crouchHeight = 0.2f;
    public float jumpForce = 5f;
    public Vector2 inputScaling = Vector2.one;
    private Rigidbody rb;
    private MovementUtils u;
    private bool slideQueued = false;
    private float slideTime = 0;
    private float wallrunSpeed = 0;
    private bool wallrunning = false, canWallrun = true, wallrunEnabled = true;
    private float wallrunningTime = 0;
    private float wallrunningDuration = 1.3f;
    private Vector3 oldWallrunningNormal = Vector3.zero;
    private float oldWallrunningHeight = Mathf.Infinity;
    private PlayerLook look;
    private Vector3 prevVelocity;
    private float wallNormalResetTime = 0;
    private Tuple<Vector3, Vector3> wallData = new Tuple<Vector3, Vector3>(Vector3.zero, Vector3.zero);
    void Start() {
        rb = GetComponent<Rigidbody>();
        look = GetComponent<PlayerLook>();
        u = GetComponent<MovementUtils>();
        u.onJump += Jump;
        u.onEnterGround += () => {
            oldWallrunningNormal = Vector3.zero;
            oldWallrunningHeight = Mathf.Infinity;
            wallrunEnabled = true;
            Debug.Log("wallrunENABLED");
            MAX_SPEED = 11f;
        };
        u.onCrouchInput += () => {
            slideQueued = true;
        };
    }

    void Jump() {
        Vector3 wish = transform.TransformDirection(new Vector3(u.dirInput.x * inputScaling.x, 0, u.dirInput.y * inputScaling.y));
        if (!u.grounded && u.dirInput.magnitude > 0) {
            var radians = Vector3.Angle(u.xzVelocity(), wish) * Mathf.Deg2Rad;
            var mag = Mathf.Max((0.4f * Mathf.Cos((radians * u.xzVelocity().magnitude) / 50) + 0.6f) * u.xzVelocity().magnitude, 10);
            MAX_SPEED = mag;
            rb.velocity = ((wish * Mathf.Clamp(Mathf.Abs(Vector3.Dot(rb.velocity.normalized, wish)) + .5f, .5f, 1)) + rb.velocity.normalized).normalized * mag;// * Mathf.Cos();
        }
        rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);
        // Debug.Log("Jump");

        if (wallrunning) {
            Vector3 wallDir = Vector3.Cross(u.wallHit.normal, Vector3.up);
            float wallSide = Vector3.Dot(wallDir, (transform.forward * Mathf.Sign(u.signedDirInput.y) + transform.right * u.signedDirInput.x).normalized);
            Vector3 newVel = wallDir.normalized * (wallrunSpeed - 3) * Mathf.Sign(wallSide) + u.wallHit.normal * 5;

            if (Vector3.Dot(u.wallHit.normal, transform.forward) < -0.7f) {
                newVel = u.wallHit.normal * 4f;
            }
            newVel.y = jumpForce;
            rb.velocity = newVel;

            canWallrun = false;
            wallrunEnabled = false;
            Invoke("EnableWallrun", .3f);
            oldWallrunningHeight -= 0.4f;
        }
    }

    void EnableWallrun() { wallrunEnabled = true; Debug.Log("wallrunENABLED"); u.onEnterGround -= EnableWallrun; }
    // bool setWallrunInvoked = false;
    // void SetWallrun() { wallrunning = false; setWallrunInvoked = false; }

    void Update() {
        Vector3 wish = transform.TransformDirection(new Vector3(u.dirInput.x * inputScaling.x, 0, u.dirInput.y * inputScaling.y));

        if (Physics.Raycast(transform.position, -Vector3.up, 1.1f, LayerMask.GetMask("Default")))
            look.SetTargetHeight(u.crouchInput ? crouchHeight : 1f);

        var upRaycast = Physics.Raycast(transform.position, Vector3.up, .4f, LayerMask.GetMask("Default"));
        if (upRaycast) {
            u.crouchInput = true;
        }
        u.capsuleCollider.height = u.crouchInput ? .5f : 2f;
        u.capsuleCollider.center = new Vector3(0, u.crouchInput ? -.5f : 0, 0);
        if (u.WallCheck() && canWallrun) {
            if (!wallrunning) {
                u.JumpReset();
                wallrunning = true;
                wallrunningTime = 0;
                wallrunSpeed = Mathf.Max(u.xzVelocity().magnitude, 13f);
                rb.velocity = new Vector3(rb.velocity.x, Mathf.Max(rb.velocity.y, -1f)/2, rb.velocity.z);
                wallNormalResetTime = 0;
                wallData = new Tuple<Vector3, Vector3>(u.wallHit.normal, u.wallHit.point);
                Debug.Log("wallrunSTART");
            }

            Vector3 wallDir = Vector3.Cross(wallData.Item1, Vector3.up);
            Vector3 smoothWish = transform.TransformDirection(new Vector3(u.smoothDirInput.x * inputScaling.x, 0, u.smoothDirInput.y * inputScaling.y));
            Vector3 wallSpaceWish = new Vector3(Vector3.Dot(smoothWish, wallDir), 0, Vector3.Dot(smoothWish, wallData.Item1));

            bool eject = false;
            if (wallData.Item1 != u.wallHit.normal) {
                if (Vector3.Angle(wallData.Item1, u.wallNormal) > 90) {
                    eject = true;
                    rb.velocity = (u.wallHit.normal + wallData.Item1) * 3;
                }
                wallData = new Tuple<Vector3, Vector3>(u.wallHit.normal, u.wallHit.point);
            }

            if (wallrunningTime > wallrunningDuration || Input.GetKeyDown(KeyCode.LeftControl) || eject || smoothWish.magnitude < 0.1f) {
                canWallrun = false;
                wallrunEnabled = false;
                if (smoothWish.magnitude > 0.1f)
                    rb.velocity += u.wallHit.normal * 4;
                oldWallrunningHeight -= 0.4f;
                wallData = new Tuple<Vector3, Vector3>(Vector3.zero, Vector3.zero);
                Debug.Log("exit wallrun");
                return;
            }

            // get the wishdir in "wall space", where x is the value of wishdir along the wall direction, and z is the value of wishdir perpendicular to the wall direction
            Vector3 stickVelocity = (-wallData.Item1 * 300 * Time.deltaTime * Vector3.Distance(transform.position-(wallData.Item1*u.capsuleCollider.radius), u.wallHit.point));
            Vector3 wallrunVelocity = wallDir * wallSpaceWish.x * Mathf.Lerp(wallrunSpeed, 15, 5 * wallrunningTime) + stickVelocity;
            wallrunVelocity.y = rb.velocity.y;
            oldWallrunningHeight = u.wallHit.point.y;
            look.SetTargetDutch(Mathf.Lerp(look.targetDutch, -15 * Mathf.Clamp01((wallrunningDuration+.5f-wallrunningTime)/wallrunningDuration) * Vector3.Dot(wallDir, transform.forward), 50*Time.deltaTime));
            wallNormalResetTime = Mathf.Max(wallNormalResetTime - Time.deltaTime, 0);

            rb.AddForce(-Vector3.up * 300 * Time.deltaTime, ForceMode.Acceleration);

            rb.velocity = wallrunVelocity;
            wallrunningTime += Time.deltaTime;
            return;
        }
        canWallrun = u.WallCheck() && (oldWallrunningNormal != u.wallHit.normal ? true : oldWallrunningHeight > u.wallHit.point.y) && wallrunEnabled && !Input.GetKey(KeyCode.LeftControl);// oldWallrunningHeight > u.wallHit.point.y 

        if (wallrunning) {
            Debug.Log("wallrunOFF " + wallrunning);
            wallrunning = false;
            Invoke("EnableWallrun", .3f);
            oldWallrunningHeight -= 0.4f;
            look.SetTargetDutch(0);
        }

        // Debug.Log((oldWallrunningNormal != u.wallHit.normal ? true : oldWallrunningHeight > u.wallHit.point.y));
        
        u.onJump -= Jump;
        u.onJump += Jump;

        if (u.crouchInput && u.grounded) {
            if (slideTime < .2f) {
                wish = u.xzVelocity().normalized;
            }else {
                wish = Vector3.zero;
            }
        }

        if (u.grounded && rb.velocity.magnitude != 0) {
            if (slideQueued && prevVelocity.magnitude > 5) {
                slideQueued = false;
                slideTime = 0;
            }
            float drop = rb.velocity.magnitude * (u.crouchInput ? 0.2f : friction) * Time.deltaTime;
            // float drop = rb.velocity.magnitude * friction * Time.deltaTime;

            float yVel = rb.velocity.y;
            rb.velocity *= Mathf.Max(rb.velocity.magnitude - drop, 0) / rb.velocity.magnitude; 
            rb.velocity = new Vector3(rb.velocity.x, yVel, rb.velocity.z);
        }

        var current_speed = Vector3.Dot(rb.velocity, wish);
        var max_speed = MAX_SPEED;
        var max_accel = (u.grounded ? (u.crouchInput && u.xzVelocity().magnitude > max_speed ? 40 : MAX_ACCELERATION) : MAX_AIR_ACCELERATION);
        if (u.crouchInput && u.grounded && u.xzVelocity().magnitude > 9)
            max_speed = MAX_SPEED * 1.5f;

        var add_speed = Mathf.Clamp(max_speed - current_speed, 0, max_accel * Time.deltaTime);

        rb.velocity = rb.velocity + add_speed * wish;
        // rb.AddForce(-Vector3.up * (u.crouchInput && u.grounded ? 60 : 20), ForceMode.Acceleration);
        rb.AddForce(-Vector3.up * ((u.crouchInput && u.groundColliders > 0) ? 1800 : 1100) * Time.deltaTime, ForceMode.Acceleration);
        slideTime += Time.deltaTime;
        prevVelocity = rb.velocity;
    }

    void OnGUI() {
        var boldStyle = new GUIStyle(GUI.skin.label);
        boldStyle.fontStyle = FontStyle.Bold;
        boldStyle.alignment = TextAnchor.UpperCenter;

        void DrawLabel(Rect rect, string value, float outline) {
            GUI.color = Color.black;

            GUI.Label(new Rect(rect.x+outline, rect.y, rect.width, rect.height), value, boldStyle);
            GUI.Label(new Rect(rect.x, rect.y+outline, rect.width, rect.height), value, boldStyle);
            GUI.Label(new Rect(rect.x-outline, rect.y, rect.width, rect.height), value, boldStyle);
            GUI.Label(new Rect(rect.x, rect.y-outline, rect.width, rect.height), value, boldStyle);

            GUI.Label(new Rect(rect.x+outline, rect.y+outline, rect.width, rect.height), value, boldStyle);
            GUI.Label(new Rect(rect.x+outline, rect.y-outline, rect.width, rect.height), value, boldStyle);
            GUI.Label(new Rect(rect.x-outline, rect.y+outline, rect.width, rect.height), value, boldStyle);
            GUI.Label(new Rect(rect.x-outline, rect.y-outline, rect.width, rect.height), value, boldStyle);
            GUI.color = Color.white;
            GUI.Label(rect, value, boldStyle);
        }
        // get screen rect
        if(speedometer){
         DrawLabel(new Rect(Screen.width/2 - 50, Screen.height/2 + 30, 100, 20), "Speed: " + (int)Mathf.Round(rb.velocity.magnitude*100)/50, 1);
         DrawLabel(new Rect(Screen.width/2 - 50, Screen.height/2 + 60, 100, 20), "HSpeed: " + (int)Mathf.Round(u.xzVelocity().magnitude*100)/50, 1);
        }
        
        // Texture2D texture = new Texture2D(1, 1);
        // texture.SetPixel(0,0,Color.white);
        // texture.Apply();
        // GUI.skin.box.normal.background = texture;
        // GUI.Box(new Rect(Screen.width/2 - 1, Screen.height/2 - 2, 2, 2), GUIContent.none);
    }
}
