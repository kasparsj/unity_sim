using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

// damping = how much the wheel RPM is limited; high damping plays well with high torque

public class KalmanVehicle : MonoBehaviour
{
    [SerializeField]
    private WheelCollider flWheel, frWheel, blWheel, brWheel;

    [SerializeField]
    private Transform rootBone;

    [SerializeField]
    private Transform flSuspBone, frSuspBone, blSuspBone, brSuspBone;

    [SerializeField]
    private Transform flTurnBone, frTurnBone, blTurnBone, brTurnBone;

    [SerializeField]
    private Transform flWheelBone, frWheelBone, blWheelBone, brWheelBone;
    [SerializeField]
    
    private const float unitTorque = 8; // torque per 1 m/s
    private const float manualSpeed = 1.0F; // WSAD move speed; m/s
    private const float manualTurnRadius = 1.0F; // WSAD turn radius; m
    private const float maxTurnSpeed = 90; // turning speed of a wheel; deg/s

    // Dictionary that contains original rotations and positions of various bones.
    private Dictionary<Transform, Quaternion> ogBoneRots = new Dictionary<Transform, Quaternion>();
    private Dictionary<Transform, Vector3> ogSupToWheelVecs = new Dictionary<Transform, Vector3>();

    private WheelStates wheelStates;
    private PID flTurnPid, frTurnPid, blTurnPid, brTurnPid;
    private PID flTorquePid, frTorquePid, blTorquePid, brTorquePid;

    private static int lightsCounter = 0;
    Camera[] allCameras;
    private static int camCounter = 0;

    private void OnMove(InputValue value)
    {
        Vector2 input = value.Get<Vector2>();
        // Scale up diagonal input.
        input = Vector2.Max(Vector2.Min(1.414F * input, Vector2.one), -Vector2.one);

        float turnRadius = -manualTurnRadius / input.x;
        float linearVelocity = manualSpeed;
        float angularVelocity = manualSpeed / turnRadius;

        Vector2[] turnVectors = new Vector2[]
        {
            new Vector2(-0.33F, 0.4F),
            new Vector2(0.33F, 0.4F),
            new Vector2(-0.33F, -0.4F),
            new Vector2(0.33F, -0.4F)
        };

        for (int i = 0; i < 4; i++)
        {
            float robotRadius = 0.5F;
            Vector2 linearVector = new Vector2(linearVelocity, 0);
            Vector2 angularVector = turnVectors[i] / turnVectors[i].magnitude * angularVelocity * robotRadius;
            Vector2 resultantVector = angularVector + linearVector;
            float velocity = resultantVector.magnitude;
            float angle = Mathf.Atan2(resultantVector.y, resultantVector.x);

            switch (i)
            {
                case 0:
                    wheelStates.FrontLeft = new WheelState
                    {
                        Velocity = velocity * input.y,
                        Angle = angle
                    };
                    break;
                case 1:
                    wheelStates.FrontRight = new WheelState
                    {
                        Velocity = velocity * input.y,
                        Angle = angle
                    };
                    break;
                case 2:
                    wheelStates.BackLeft = new WheelState
                    {
                        Velocity = velocity * input.y,
                        Angle = angle
                    };
                    break;
                case 3:
                    wheelStates.BackRight = new WheelState
                    {
                        Velocity = velocity * input.y,
                        Angle = angle
                    };
                    break;
            }
        }
    }

    private void OnLights(InputValue value)
    {
        KalmanUeuos ueuos = GetComponentInChildren<KalmanUeuos>();
        if (ueuos != null)
        {
            switch (lightsCounter%7) {
                case 0:
                    ueuos.SetColor(Color.red);
                    break;
                case 1:
                    ueuos.SetState(KalmanUeuos.STATE_OFF);
                    break;
                case 2:
                    ueuos.SetState(KalmanUeuos.STATE_AUTONOMY);
                    break;
                case 3:
                    ueuos.SetState(KalmanUeuos.STATE_TELEOP);
                    break;
                case 4:
                    ueuos.SetState(KalmanUeuos.STATE_FINISHED);
                    break;
                case 5:
                    ueuos.SetEffect(KalmanUeuos.EFFECT_BOOT);
                    break;
                case 6:
                    ueuos.SetEffect(KalmanUeuos.EFFECT_RAINBOW);
                    break;
            }
        }
        lightsCounter++;
    }

    private void OnCamera(InputValue value)
    {
        string[] camNames = {"FollowCamera", "FrontCamera", "LeftCamera", "RightCamera", "BackCamera"};
        if (allCameras == null) {
            var followCam = GameObject.Find("FollowCamera").GetComponent<Camera>();
            allCameras = (GetComponentsInChildren<Camera>()).Union(new Camera[]{followCam}).ToArray();
        }
        camCounter++;
        var camera = camNames[camCounter%camNames.Length];
        foreach (Camera childCamera in allCameras)
        {
            if (childCamera.enabled && childCamera.gameObject.name != camera) {
                childCamera.enabled = false;
                //Debug.Log(childCamera.gameObject.name + " disabled");
            }
            else if (childCamera.gameObject.name == camera) {
                childCamera.enabled = true;
                //Debug.Log(camera + " enabled");
            }
        }
    }

    private async void Start()
    {
        // Override center of mass for better handling.
        GetComponent<Rigidbody>().centerOfMass = new Vector3(0, 0.2F, 0);

        // Zero-out wheel states.
        wheelStates = new WheelStates
        {
            FrontLeft = new WheelState
            {
                Velocity = 0,
                Angle = 0
            },
            FrontRight = new WheelState
            {
                Velocity = 0,
                Angle = 0
            },
            BackLeft = new WheelState
            {
                Velocity = 0,
                Angle = 0
            },
            BackRight = new WheelState
            {
                Velocity = 0,
                Angle = 0
            }
        };

        // Initialize PIDs.
        flTurnPid = frTurnPid = blTurnPid = brTurnPid = new PID(0.2F, 0, 0);
        flTorquePid = frTorquePid = blTorquePid = brTorquePid = new PID(1, 0, 0);
    }

    private void FixedUpdate()
    {
        flWheel.steerAngle += Mathf.Clamp(flTurnPid.Update(flWheel.steerAngle, -wheelStates.FrontLeft.Angle * Mathf.Rad2Deg, Time.fixedDeltaTime), -maxTurnSpeed * Time.fixedDeltaTime, maxTurnSpeed * Time.fixedDeltaTime);
        frWheel.steerAngle += Mathf.Clamp(frTurnPid.Update(frWheel.steerAngle, -wheelStates.FrontRight.Angle * Mathf.Rad2Deg, Time.fixedDeltaTime), -maxTurnSpeed * Time.fixedDeltaTime, maxTurnSpeed * Time.fixedDeltaTime);
        blWheel.steerAngle += Mathf.Clamp(blTurnPid.Update(blWheel.steerAngle, -wheelStates.BackLeft.Angle * Mathf.Rad2Deg, Time.fixedDeltaTime), -maxTurnSpeed * Time.fixedDeltaTime, maxTurnSpeed * Time.fixedDeltaTime);
        brWheel.steerAngle += Mathf.Clamp(brTurnPid.Update(brWheel.steerAngle, -wheelStates.BackRight.Angle * Mathf.Rad2Deg, Time.fixedDeltaTime), -maxTurnSpeed * Time.fixedDeltaTime, maxTurnSpeed * Time.fixedDeltaTime);

        flWheel.motorTorque += flTorquePid.Update(flWheel.motorTorque, unitTorque * wheelStates.FrontLeft.Velocity, Time.fixedDeltaTime);
        frWheel.motorTorque += frTorquePid.Update(frWheel.motorTorque, unitTorque * wheelStates.FrontRight.Velocity, Time.fixedDeltaTime);
        blWheel.motorTorque += blTorquePid.Update(blWheel.motorTorque, unitTorque * wheelStates.BackLeft.Velocity, Time.fixedDeltaTime);
        brWheel.motorTorque += brTorquePid.Update(brWheel.motorTorque, unitTorque * wheelStates.BackRight.Velocity, Time.fixedDeltaTime);

        // Brake if the vehicle should not move.
        BrakeWheelIfStationary(flWheel);
        BrakeWheelIfStationary(frWheel);
        BrakeWheelIfStationary(blWheel);
        BrakeWheelIfStationary(brWheel);

        // float vel0 = (flWheel.transform.position - wheelPrevPositions[0]).magnitude / Time.fixedDeltaTime;
        // Debug.Log($"boost0 = {motorTorqueBoostsSmooth[0]}, vel0 = {vel0}");

        // for (int i = 0; i < 4; i++)
        // {
        //     Vector3 wheelPos = i switch
        //     {
        //         0 => flWheel.transform.position,
        //         1 => frWheel.transform.position,
        //         2 => blWheel.transform.position,
        //         3 => brWheel.transform.position,
        //         _ => Vector3.zero
        //     };
        //     Vector3 wheelPrevPos = wheelPrevPositions[i];
        //     wheelPrevPositions[i] = wheelPos;

        //     float wantedVel = i switch
        //     {
        //         0 => wheelStates.FrontLeft.Velocity,
        //         1 => wheelStates.FrontRight.Velocity,
        //         2 => wheelStates.BackLeft.Velocity,
        //         3 => wheelStates.BackRight.Velocity,
        //         _ => 0
        //     };
        //     float wheelVel = (wheelPos - wheelPrevPos).magnitude / Time.fixedDeltaTime;

        //     float P = 0.1F * (Mathf.Abs(wantedVel) - Mathf.Abs(wheelVel));
        //     motorTorqueBoostsSmooth[i] += P;
        //     motorTorqueBoostsSmooth[i] = Mathf.Clamp(motorTorqueBoostsSmooth[i], 0.5F, 2);
        // }
    }

    private void BrakeWheelIfStationary(WheelCollider wheel)
    {
        if (Mathf.Abs(wheel.motorTorque) < 0.1F)
        {
            wheel.brakeTorque = 1;
        }
        else
        {
            wheel.brakeTorque = 0;
        }
    }

    private void Update()
    {
        CopyWheelTransform(flWheel, flSuspBone, flTurnBone, flWheelBone, false, true);
        CopyWheelTransform(frWheel, frSuspBone, frTurnBone, frWheelBone, true, true);
        CopyWheelTransform(blWheel, blSuspBone, blTurnBone, blWheelBone, false, false);
        CopyWheelTransform(brWheel, brSuspBone, brTurnBone, brWheelBone, true, false);
    }

    private void CopyWheelTransform(WheelCollider wheel, Transform suspBone, Transform turnBone, Transform wheelBone, bool rightSide, bool front)
    {
        // Put turn and suspension rotations in the dictionary if needed.
        if (!ogBoneRots.ContainsKey(turnBone))
        {
            ogBoneRots.Add(turnBone, turnBone.localRotation);
        }
        if (!ogBoneRots.ContainsKey(suspBone))
        {
            ogBoneRots.Add(suspBone, suspBone.localRotation);
        }
        // Also remember original positions of wheel bones relative to suspension bones.
        if (!ogSupToWheelVecs.ContainsKey(wheelBone))
        {
            Matrix4x4 worldToLocal = rootBone.parent.worldToLocalMatrix;
            Vector3 from = worldToLocal.MultiplyPoint(suspBone.position);
            Vector3 to = worldToLocal.MultiplyPoint(wheelBone.position);
            ogSupToWheelVecs.Add(wheelBone, to - from);
        }

        // Get the wheel's world position and rotation.
        Vector3 position;
        Quaternion rotation;
        wheel.GetWorldPose(out position, out rotation);

        // Apply wheel rotation about wheel bone's up axis.
        float angularSpeed = wheel.rpm / 60 * 360 * (rightSide ? 1 : -1);
        // if (Mathf.Abs(angularSpeed) > 10)
        // {
        wheelBone.Rotate(Vector3.up, angularSpeed * Time.deltaTime);
        // }

        // Apply turn steering about turn bone's up axis.
        Quaternion newRot = ogBoneRots[turnBone] * Quaternion.Euler(0, -wheel.steerAngle, 0);
        // turnBone.localRotation = Quaternion.Slerp(turnBone.localRotation, newRot, Time.deltaTime * 10.0F);
        turnBone.localRotation = newRot;

        // Point the suspension bone at the wheel. Rotate only about the up axis.
        Vector3 ogToWheel = ogSupToWheelVecs[wheelBone];
        Matrix4x4 worldToLocal2 = rootBone.parent.worldToLocalMatrix;
        Vector3 from2 = worldToLocal2.MultiplyPoint(suspBone.position);
        Vector3 to2 = worldToLocal2.MultiplyPoint(position);
        Vector3 toWheel = to2 - from2;
        float angle = Vector3.SignedAngle(ogToWheel, toWheel, Vector3.right);

        // Apply rotation about suspension bone's up axis.
        newRot = ogBoneRots[suspBone] * Quaternion.Euler(0, angle * (rightSide ? -1 : 1) * (front ? 2 : 1), 0);
        suspBone.localRotation = Quaternion.Slerp(suspBone.localRotation, newRot, Time.deltaTime * 10.0F);
    }
}
