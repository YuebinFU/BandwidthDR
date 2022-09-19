using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Samples;
using Ubiq;
using Ubiq.Logging;
using Ubiq.Rooms;

public class CubeScript : NetworkBehaviour
{
    // public NetworkId Id { get; set; } = new NetworkId();
    // NetworkContext ctx;
    // NetworkScene ns;
    public bool owner = true;
    public float blendTime = 0.1f;
    private Rigidbody body;
    private LogEmitter LE;
    private LatencyMeter LM;
    private float timeRecordStart = -1;
    private float timeRecordEnd = -1;

    // Below varibles are for Dead Reckoning, simple implementation

    // Record the position and rotation in the frame before. It may be used to compute the local speed.
    Vector3 beforePosition;

    // Record the position and rotation in the frame before. It may be used to compute the local speed.
    Vector3 remotePosition;

    // the speed and direction for object movement of local and remote ownership
    float speed;
    float oldSpeed;
    // float acceleration;
    Vector3 direction;

    // for remote using
    float remoteSpeed;
    Vector3 remoteDirection;
    // float OldAcceleration;

    // thresholds for blending
    const float epilsonForPosition = 0.2F;
    const float epilsonForSpeed = 0.3F;
    const float epilsonForBlendingDistance = 0.5F;
    
    public struct Message
    {
        public TransformMessage transform;
        public float speedForMessage;
        public Vector3 directionForMessage;
        // public float accelerationForMessage;
        /* second order
        public Message(Transform transform, float speedForMessage, Vector3 directionForMessage, float accelerationForMessage)
        {
            this.transform = new TransformMessage(transform);
            this.speedForMessage = speedForMessage;
            this.directionForMessage = directionForMessage;
            this.accelerationForMessage = accelerationForMessage;
        }
        */
        public Message(Transform transform, float speedForMessage, Vector3 directionForMessage)
        {
            this.transform = new TransformMessage(transform);
            this.speedForMessage = speedForMessage;
            this.directionForMessage = directionForMessage;
        }
    }

    protected override void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        /* This function is used to process message from local ownership.
         * It will update remote ownership's position, rotation, speed and direction.
         * For better visual performance, we will not update them directly; but we will use converge scheme to blend it
         */
        var msg = message.FromJson<Message>();

        // blending
        blendStart = Time.time;
        blendEnd = Time.time + blendTime;
        initialLocalPosition = transform.position;
        initialRemotePosition = msg.transform.position;
        localVelocity = speed * direction;
        remoteVelocity = msg.speedForMessage * msg.directionForMessage;

        speed = remoteSpeed;
        direction = remoteDirection;
        remoteSpeed = msg.speedForMessage;
        remoteDirection = msg.directionForMessage;
        // OldAcceleration = acceleration;
        // acceleration = msg.accelerationForMessage;

        if (Vector3.Distance(msg.transform.position, transform.position) > epilsonForBlendingDistance)
        {
            // if the distance is too large, we set the position directly
            transform.localPosition = msg.transform.position;
            transform.localRotation = msg.transform.rotation;
        }
    }

    float blendStart = -1;
    float blendEnd = -1;
    Vector3 initialLocalPosition;
    Vector3 initialRemotePosition;
    Vector3 localVelocity;
    Vector3 remoteVelocity;


    // Start is called before the first frame update
    protected override void Started()
    {
        body = GetComponent<Rigidbody>();
        // ctx = NetworkScene.Register(this);
        // NetworkScene.FindNetworkScene(this);
        LE = new ExperimentLogEmitter(this);
        LM = networkScene.GetComponent<LatencyMeter>();
        speed = 0;
        oldSpeed = 0;
        // acceleration = 0;
        // OldAcceleration = 0;
        direction = new Vector3(0, 0, 0);

        // intialize the speed and direction
        remoteSpeed = speed;
        remoteDirection = direction;

        // intialize the position and rotation record
        beforePosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        remotePosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);
    }

    // Update is called once per frame
    void Update()
    {
        timeRecordEnd = Time.time;
        if (owner)
        {
            //for local ownership

            // update speed, direction and acceleration
            speed = Vector3.Distance(this.transform.position, beforePosition);
            direction = (this.transform.position - beforePosition).normalized;

            // update remote position
            remoteVelocity = remoteSpeed * remoteDirection;
            var remoteTrajectory = initialRemotePosition + remoteVelocity * (Time.time - blendStart);

            //for second-order model
            // remoteTrajectory += remoteDirection * acceleration * (Time.time - blendStart) * (Time.time - blendStart) / 2;

            if (timeRecordEnd - timeRecordStart >= 0.1f)
            {
                // update old speed and remote speed 10 times per second
                // remoteSpeed += acceleration;
                oldSpeed = speed;
            }

            // in four cases we will send packets to remote ownership
            // first, for blending time;
            // second, the difference value between local and remote position exceed the threshold for position; 
            // third, the difference value between local and remote speed exceed the threshold for speed
            if (blendStart < 0 || 
                Vector3.Distance(transform.position,remoteTrajectory) > epilsonForPosition ||
                Mathf.Abs(speed - remoteSpeed) > epilsonForSpeed
                )
            {
                // SendJson(new Message(transform, speed, direction, speed - oldSpeed));
                SendJson(new Message(transform, speed, direction));
                blendStart = Time.time;
                initialRemotePosition = transform.position;
                remoteSpeed = speed;
                // acceleration = speed - oldSpeed;

                if (Vector3.Distance(remoteTrajectory, transform.position) > epilsonForBlendingDistance)
                {
                    // if the distance is too large, we set the position directly
                    remotePosition = transform.position;
                }
                // var mes = new Message(transform, speed, direction, speed - oldSpeed);
                var mes = new Message(transform, speed, direction);
                LE.Log("Send Message due to error exceed threshold", System.Runtime.InteropServices.Marshal.SizeOf(mes));
            }
        }
        else
        {
            // for remote ownership

            // disable the physcial simulation if the ownership is remote
            body.isKinematic = true;



            if (blendEnd < 0)
            {
                initialRemotePosition = transform.position;
                blendStart = Time.time;
            }

            localVelocity = speed * direction;
            remoteVelocity = remoteSpeed * remoteDirection;
            var localTrajectory = initialLocalPosition + localVelocity * (Time.time - blendStart);
            var remoteTrajectory = initialRemotePosition + remoteVelocity * (Time.time - blendStart);

            // localTrajectory += direction * OldAcceleration * (Time.time - blendStart) * (Time.time - blendStart) / 2;
            // remoteTrajectory += direction * acceleration * (Time.time - blendStart) * (Time.time - blendStart) / 2;
            // for second-order model

            var t = Mathf.Clamp01((Time.time - blendStart) / (blendEnd - blendStart));

            transform.position = localTrajectory * (1 - t) + remoteTrajectory * t; // blending

            if (timeRecordEnd - timeRecordStart >= 0.1f)
            {
                // update old speed and remote speed 10 times per second
                // remoteSpeed += acceleration;
                // speed += acceleration;
            }
            /*
            if (remoteSpeed == 0 && acceleration == 0)
            {
                transform.localPosition = initialRemotePosition;
                // transform.localRotation = initialRemoteRotation;
                speed = 0;
            }
            */
            // printUnit("update in remote", transform.position.ToString());
        }

        LE.Log("CubeScript", this.transform.position.x, this.transform.position.y, this.transform.position.z, owner, networkId);

        // Logging Latency
        // reset timerecord
        if (timeRecordEnd - timeRecordStart >= 0.1f)
        {
            timeRecordStart = Time.time;
        }

        // update the position of last frame to compute the moving speed and direction, and rotation to check if there is a change of rotation
        beforePosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);
    }

    protected override void OnSpawn(bool local)
    {
        owner = local;
    }

}
