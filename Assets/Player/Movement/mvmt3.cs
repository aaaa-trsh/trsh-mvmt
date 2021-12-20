using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class mvmt3 : MonoBehaviour
{
    public float maxSpeed = 11f;
    public float accel = 100f;
    public float airAccel = 100f;
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
            maxSpeed = 11f;
        };
        u.onExitGround += () => {
            if (u.crouchInput)
                slideQueued = true;
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
            rb.velocity = ((wish * Mathf.Clamp(Mathf.Abs(Vector3.Dot(rb.velocity.normalized, wish)) + .5f, .5f, 1)) + rb.velocity.normalized).normalized * mag;// * Mathf.Cos();
        }

        if (wallrunning) {
            Vector3 wallDir = Vector3.Cross(u.wallHit.normal, Vector3.up);
            float wallSide = Vector3.Dot(wallDir, (transform.forward * Mathf.Sign(u.signedDirInput.y) + transform.right * u.signedDirInput.x).normalized);
            Vector3 newVel = wallDir.normalized * Mathf.Lerp(wallrunSpeed, 15, 2 * wallrunningTime) * Mathf.Sign(wallSide) + u.wallHit.normal * 5;

            if (Vector3.Dot(u.wallHit.normal, transform.forward) < -0.7f)
                newVel = u.wallHit.normal * 4f;
            rb.velocity = newVel;

            WallrunExit();
            Invoke("EnableWallrun", .3f);
        }

        rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);
    }

    void EnableWallrun() { wallrunEnabled = true; Debug.Log("wallrunENABLED"); u.onEnterGround -= EnableWallrun; }
    
    void WallrunStart() {
        u.JumpReset();
        wallrunning = true;
        wallrunningTime = 0;
        wallrunSpeed = Mathf.Max(u.xzVelocity().magnitude, 13f);
        rb.velocity = new Vector3(rb.velocity.x, Mathf.Max(rb.velocity.y, -1f)/2, rb.velocity.z);
        wallData = new Tuple<Vector3, Vector3>(u.wallHit.normal, u.wallHit.point);
    }
    bool WallrunCheck() {
        bool eject = false;
        if (wallData.Item1 != u.wallHit.normal) {
            // if (Vector3.Angle(wallData.Item1, u.wallNormal) > 90) {
            //     eject = true;
            //     rb.velocity = (u.wallHit.normal + wallData.Item1) * 3;
            // }
            wallData = new Tuple<Vector3, Vector3>(u.wallHit.normal, u.wallHit.point);
        }
        Vector3 wallDir = Vector3.Cross(wallData.Item1, Vector3.up);
        return wallrunningTime > wallrunningDuration ||
                Input.GetKey(KeyCode.LeftControl) || 
                eject;
    }
    void WallrunExit() {
        canWallrun = false;
        wallrunEnabled = false;
        oldWallrunningHeight -= 0.4f;
        oldWallrunningNormal = wallData.Item1;
    }

    void WallrunReset() {
        wallrunning = false;
        Invoke("EnableWallrun", .3f);
        oldWallrunningHeight -= 0.4f;
        look.SetTargetDutch(0);
        wallData = new Tuple<Vector3, Vector3>(Vector3.zero, Vector3.zero);
    }
    void FixedUpdate() {
        if (canWallrun) {
            if (!wallrunning) {
                WallrunStart();
                Debug.Log("wallrunSTART");
            }

            bool wallEnd = !Physics.Raycast(transform.position + Vector3.Cross(wallData.Item1, Vector3.up)/7, -u.wallHit.normal, u.capsuleCollider.radius + 0.5f, u.whatIsGround);
            if (WallrunCheck() || u.grounded || wallEnd) {
                if (!wallEnd)
                    rb.velocity += u.wallHit.normal * 4;
                WallrunExit();
                Debug.Log("exit wallrun" + " " + rb.velocity);
                return;
            }

            Vector3 wallDir = Vector3.Cross(wallData.Item1, Vector3.up);
            Vector3 smoothWish = transform.TransformDirection(new Vector3(u.smoothDirInput.x * inputScaling.x, 0, u.smoothDirInput.y * inputScaling.y));
            Vector3 wallSpaceWish = new Vector3(Vector3.Dot(smoothWish, wallDir), 0, Vector3.Dot(smoothWish, wallData.Item1));
            Vector3 stickVelocity = (-wallData.Item1 * 300 * Time.fixedDeltaTime * Vector3.Distance(transform.position-(wallData.Item1*u.capsuleCollider.radius), u.wallHit.point));
            Vector3 wallrunVelocity = wallDir * wallSpaceWish.x * Mathf.Lerp(wallrunSpeed, 15, 2 * wallrunningTime) + stickVelocity;
            wallrunVelocity.y = rb.velocity.y;
            oldWallrunningHeight = u.wallHit.point.y;
            look.SetTargetDutch(Mathf.Lerp(look.targetDutch, -15 * Mathf.Clamp01((wallrunningDuration+.5f-wallrunningTime)/wallrunningDuration) * Vector3.Dot(wallDir, transform.forward), 50*Time.fixedDeltaTime));

            rb.velocity = wallrunVelocity;
            rb.AddForce(-Vector3.up * 300 * Time.fixedDeltaTime, ForceMode.Acceleration);
            wallrunningTime += Time.fixedDeltaTime;
            return;
        }

        canWallrun = u.WallCheck() && (oldWallrunningNormal == Vector3.zero ? true : Mathf.Abs(Vector3.Angle(oldWallrunningNormal, u.wallHit.normal)) > 25f ? true : oldWallrunningHeight > u.wallHit.point.y) && wallrunEnabled;// oldWallrunningHeight > u.wallHit.point.y 

        if (wallrunning) {
            Debug.Log("wallrunOFF " + wallrunning + " " + rb.velocity);
            WallrunReset();
        }

        float _maxSpeed = maxSpeed;
        if (u.crouchInput && u.grounded && u.xzVelocity().magnitude > 5)
            _maxSpeed *= 1.5f;
        float yVel = rb.velocity.y;
        Vector3 wish = transform.TransformDirection(new Vector3(u.dirInput.x * inputScaling.x, 0, u.dirInput.y * inputScaling.y));
        if (u.grounded) {
            // slide
            if (Physics.Raycast(transform.position, -Vector3.up, 1.1f, LayerMask.GetMask("Default")))
                look.SetTargetHeight(u.crouchInput ? crouchHeight : 1f);

            if (Physics.Raycast(transform.position, Vector3.up, .4f, LayerMask.GetMask("Default"))) 
                u.crouchInput = true;

            u.capsuleCollider.height = u.crouchInput ? .5f : 2f;
            u.capsuleCollider.center = new Vector3(0, u.crouchInput ? -.5f : 0, 0);

            if (slideQueued && prevVelocity.magnitude > 5) {
                slideQueued = false;
                slideTime = 0;
            }

            if (u.crouchInput) {
                wish = slideTime < 0.2f ? u.xzVelocity().normalized : Vector3.zero;
                slideTime += Time.fixedDeltaTime;
            }

            // friction
            if (rb.velocity.magnitude != 0) {
                float drop = rb.velocity.magnitude * (u.crouchInput ? 0.2f : friction) * Time.fixedDeltaTime;
                rb.velocity *= Mathf.Max(rb.velocity.magnitude - drop, 0) / rb.velocity.magnitude; 
            }

            // core movement
            var curSpeed = Vector3.Dot(rb.velocity, wish);
            var maxAccel = (u.crouchInput && u.xzVelocity().magnitude > _maxSpeed ? 40 : accel);
            var addSpeed = Mathf.Clamp(_maxSpeed - curSpeed, 0, maxAccel * Time.fixedDeltaTime * _maxSpeed);
            rb.velocity += addSpeed * wish;
        }
        else {
            // core movement
            var curSpeed = Vector3.Dot(rb.velocity, wish);
            var maxAccel = airAccel;
            // var addSpeed = Mathf.Clamp(_maxSpeed - curSpeed, 0, maxAccel * Time.deltaTime);
            // rb.velocity = rb.velocity + addSpeed * wish;

            float addSpeed = _maxSpeed - curSpeed;
            if (addSpeed > 0f) {
                float accel = Mathf.Min(maxAccel * Time.fixedDeltaTime * _maxSpeed, addSpeed);
                rb.velocity += accel * wish;
            }
        }
        rb.velocity = new Vector3(rb.velocity.x, yVel, rb.velocity.z);
        rb.AddForce(-Vector3.up * ((u.crouchInput && u.groundColliders > 0) ? 1900 : 1200) * Time.fixedDeltaTime, ForceMode.Acceleration);
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
    }
}
