

// mvmt3
using System;
using UnityEngine;

public class mvmt3_v52 : MonoBehaviour {
    public float maxSpeed = 11f;
    public float accel = 100f;
    public float airAccel = 100f;
    public float friction = 5f;
    public bool speedometer;
    public float crouchHeight = 0.2f;
    public float jumpForce = 5f;
    public Vector2 inputScaling = Vector2.one;
    private Rigidbody rb;
    private MovementUtils u;
    private bool slideQueued;
    private float slideTime;
    private float wallrunSpeed;
    private bool wallrunning;
    private bool canWallrun = true;
    private bool wallrunEnabled = true;
    private float wallrunningTime;
    private float wallrunningDuration = 1.3f;
    private Vector3 oldWallrunningNormal = Vector3.zero;
    private float oldWallrunningHeight = Mathf.Infinity;
    private PlayerLook look;
    private Vector3 prevVelocity;
    private Tuple<Vector3, Vector3> wallData = new Tuple<Vector3, Vector3>(Vector3.zero, Vector3.zero);

    private void Start() {
        rb = GetComponent<Rigidbody>();
        look = GetComponent<PlayerLook>();
        u = GetComponent<MovementUtils>();
        u.onJump += Jump;
        u.onEnterGround += () => {
            oldWallrunningNormal = Vector3.zero;
            oldWallrunningHeight = Mathf.Infinity;
            wallrunEnabled = true;
            Debug.Log("wallrunENABLED");
        };
        u.onExitGround += () => {
            if (u.crouchInput)
                slideQueued = true;
        };
        u.onCrouchInput += () => {
            slideQueued = true;
        };
        u.onReset += () => {
            WallrunExit();
            WallrunReset();
        };
    }

    private void Jump() {
        Vector3 val = transform.TransformDirection(new Vector3(u.dirInput.x * inputScaling.x, 0f, u.dirInput.y * inputScaling.y));
        Vector3 wish = u.transformify(u.dirInput);

        if (!u.grounded) {
            Vector2 dirInput = u.dirInput;
            if (dirInput.magnitude > 0f) {
                float radians = Vector3.Angle(u.xzVelocity(), val) * ((float)Math.PI / 180f);
                float mag = Mathf.Max((0.4f * Mathf.Cos((radians * u.xzVelocity().magnitude) / 50) + 0.6f) * u.xzVelocity().magnitude, 10);
                rb.velocity = ((wish * Mathf.Clamp(Mathf.Abs(Vector3.Dot(rb.velocity.normalized, wish)) + .5f, .5f, 1)) + rb.velocity.normalized).normalized * mag;// * Mathf.Cos();
            }
        }
        if (wallrunning) {
            Vector3 wallDir = Vector3.Cross(u.wallHit.normal, Vector3.up);
            float wallSide = Vector3.Dot(wallDir, (transform.forward * Mathf.Sign(u.signedDirInput.y) + transform.right * u.signedDirInput.x).normalized);
            Vector3 velocity = wallDir.normalized * Mathf.Lerp(wallrunSpeed, 15f, 5f * wallrunningTime) * Mathf.Sign(wallSide) + u.wallHit.normal * 5f;

            if (Vector3.Dot(u.wallHit.normal, transform.forward) < -0.7f)
                velocity = u.wallHit.normal * 4f;
                
            rb.velocity = velocity;
            WallrunExit();
            Invoke("EnableWallrun", 0.3f);
        }
        rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);
    }

    private void EnableWallrun() {
        wallrunEnabled = true;
        Debug.Log("wallrunENABLED");
        u.onEnterGround -= EnableWallrun;
    }

    private void WallrunStart() {
        u.JumpReset();
        wallrunning = true;
        wallrunningTime = 0f;
        wallrunSpeed = Mathf.Max(Vector3.Scale(prevVelocity, new Vector3(1, 0, 1)).magnitude, 13f);
        rb.velocity = new Vector3(rb.velocity.x, Mathf.Max(rb.velocity.y, -1f)/2, rb.velocity.z);
        wallData = new Tuple<Vector3, Vector3>(u.wallHit.normal, u.wallHit.point);
    }

    private bool WallrunCheck() {
        bool eject = false;
        if (wallData.Item1 != u.wallHit.normal) {
            // if (Vector3.Angle(wallData.Item1, u.wallNormal) > 90f) {
            //     eject = true;
            //     rb.velocity = ((u.wallHit.normal + wallData.Item1) * 3f);
            // }
            wallData = new Tuple<Vector3, Vector3>(u.wallHit.normal, u.wallHit.point);
        }
        Vector3 wallDir = Vector3.Cross(wallData.Item1, Vector3.up);
        return wallrunningTime > wallrunningDuration ||
                Input.GetKey(KeyCode.LeftControl) || 
                eject;
    }

    private void WallrunExit() {
        canWallrun = false;
        wallrunEnabled = false;
        oldWallrunningHeight -= 0.4f;
    }

    private void WallrunReset() {
        wallrunning = false;
        Invoke("EnableWallrun", 0.3f);
        oldWallrunningHeight -= 0.4f;
        look.SetTargetDutch(0f);
        wallData = new Tuple<Vector3, Vector3>(Vector3.zero, Vector3.zero);
    }

    private void Update() {
        if (canWallrun) {
            if (!wallrunning) {
                WallrunStart();
                Debug.Log("wallrunSTART");
            }
            Debug.Log(u.wallHit.normal);
            if (WallrunCheck() || u.grounded) {
                if (u.smoothDirInput.magnitude > 0.1f)
                    rb.velocity = (rb.velocity + u.wallHit.normal * 4f);
                WallrunExit();
                Debug.Log("exit wallrun");
                return;
            }
            if (u.wallHit.normal.magnitude == 0f) {
                WallrunExit();
                return;
            }
            Vector3 wallDir = Vector3.Cross(wallData.Item1, Vector3.up);
            Vector3 smoothWish = u.transformify(u.smoothDirInput);
            Vector3 wallSpaceWish = new Vector3(Vector3.Dot(smoothWish, wallDir), 0f, Vector3.Dot(smoothWish, wallData.Item1));
            Vector3 stickVelocity = -wallData.Item1 * 300f * Time.deltaTime * Vector3.Distance(transform.position - wallData.Item1 * u.capsuleCollider.radius, u.wallHit.point);
            Vector3 wallrunVelocity = wallDir * wallSpaceWish.x * Mathf.Lerp(wallrunSpeed, 15f, 5f * wallrunningTime) + stickVelocity;
            wallrunVelocity.y = rb.velocity.y;

            rb.velocity = wallrunVelocity;
            rb.AddForce(-Vector3.up * 300f * Time.deltaTime, (ForceMode)5);

            oldWallrunningHeight = u.wallHit.point.y;
            look.SetTargetDutch(Mathf.Lerp(look.targetDutch, -15f * Mathf.Clamp01((wallrunningDuration + 0.5f - wallrunningTime) / wallrunningDuration) * Vector3.Dot(wallDir, transform.forward), 50f * Time.deltaTime));
            wallrunningTime += Time.deltaTime;
            return;
        }
        // canWallrun = u.WallCheck() && (oldWallrunningNormal != u.wallHit.normal || oldWallrunningHeight > u.wallHit.point.y) && wallrunEnabled && !Input.GetKey((KeyCode)306) && u.dirInput.magnitude != 0f;
        canWallrun = u.WallCheck() && (oldWallrunningNormal == Vector3.zero ? true : Mathf.Abs(Vector3.Angle(oldWallrunningNormal, u.wallHit.normal)) > 25f ? true : oldWallrunningHeight > u.wallHit.point.y) && wallrunEnabled;
        
        if (wallrunning) {
            Debug.Log("wallrunOFF " + wallrunning);
            WallrunReset();
        }
        float speed = maxSpeed;

        float yVel = rb.velocity.y;
        Vector3 wish = u.transformify(u.dirInput);
        if (u.grounded) {
            if (u.crouchInput && u.grounded && u.xzVelocity().magnitude > 5f)
                speed *= 1.5f;
            if (Physics.Raycast(transform.position, -Vector3.up, 1.1f, u.whatIsGround))
                look.SetTargetHeight(u.crouchInput ? crouchHeight : 1f);

            if (Physics.Raycast(transform.position, Vector3.up, 0.4f, u.whatIsGround))
                u.crouchInput = true;

            u.capsuleCollider.height = u.crouchInput ? 0.5f : 2f;
            u.capsuleCollider.center = new Vector3(0f, u.crouchInput ? -0.5f : 0f, 0f);

            if (slideQueued && prevVelocity.magnitude > 5f) {
                slideQueued = false;
                slideTime = 0f;
            }
            
            if (u.crouchInput) {
                if (slideTime < 0.2f)
                    wish = Vector3.Scale(rb.velocity, new Vector3(1, 0, 1)).normalized;
                else
                    wish = Vector3.zero;
                slideTime += Time.deltaTime;
            }

            if (rb.velocity.magnitude != 0f) {
                float drop = rb.velocity.magnitude * (u.crouchInput ? 0.2f : friction) * Time.deltaTime;
                rb.velocity *= Mathf.Max(rb.velocity.magnitude - drop, 0f) / rb.velocity.magnitude;
            }
            float curSpeed = Vector3.Dot(rb.velocity, wish);
            float addSpeed = Mathf.Clamp(speed - curSpeed, 0f, accel * Time.deltaTime * speed);
            rb.velocity += addSpeed * wish;
            rb.velocity = new Vector3(rb.velocity.x, yVel, rb.velocity.z);
        }
        else {
            float curSpeed = Vector3.Dot(rb.velocity, wish);
            float addSpeed = speed - curSpeed;
            float a = airAccel;
            if (u.collisionCount > 0)
                a *= 3f;
            if (addSpeed > 0f)
                rb.velocity += Mathf.Min(a * Time.deltaTime * speed, addSpeed) * wish;
        }
        rb.AddForce(-Vector3.up * ((u.crouchInput && u.grounded) ? 1900 : 1200) * Time.deltaTime, ForceMode.Acceleration);
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
        if(speedometer) {
            DrawLabel(new Rect(Screen.width/2 - 50, Screen.height/2 + 30, 100, 20), "Speed: " + (int)Mathf.Round(rb.velocity.magnitude*100)/50, 1);
            DrawLabel(new Rect(Screen.width/2 - 50, Screen.height/2 + 60, 100, 20), "HSpeed: " + (int)Mathf.Round(u.xzVelocity().magnitude*100)/50, 1);
        }
    }
}
