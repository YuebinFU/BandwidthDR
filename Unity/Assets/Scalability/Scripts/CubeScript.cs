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
    private Rigidbody body;
    private LogEmitter LE;
    private LatencyMeter LM;
    private float timeRecord;

    // Below varibles are for Dead Reckoning, simple implementation

    // Record the position and rotation in the frame before. It may be used to compute the local speed.
    Vector3 beforePosition;

    // Record the position and rotation in the frame before. It may be used to compute the local speed.
    Vector3 remotePosition;
    Quaternion remoteRotation;

    // the speed and direction for object movement of local and remote ownership
    float speed;
    Vector3 direction;

    // for remote using
    float remoteSpeed;
    Vector3 remoteDirection;
    bool received = false;

    // thresholds for blending
    const float epilsonForSpeed = 0.5F;
    const float epilsonForPosition = 0.5F;
    const float epilsonForBlendingDistance = 0.5F;
    const float epilsonForRotation = 30F;


    public struct Message
    {
        public TransformMessage transform;
        public float speedForMessage;
        public Vector3 directionForMessage;
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
         * For better visual performance, we will not update them directly; but we will try to blend it
         * Here I just simply take average of them
         */
        var msg = message.FromJson<Message>();
        if (Vector3.Distance(msg.transform.position,transform.position) > epilsonForBlendingDistance)
        {
            // if the distance is too large, we don't blend them
            transform.localPosition = msg.transform.position;
            transform.localRotation = msg.transform.rotation;
            speed = msg.speedForMessage;
            direction = msg.directionForMessage;
        }
        else
        {
            // blending
            transform.localRotation = msg.transform.rotation;
            speed = (msg.speedForMessage + speed) / 2;
            direction = (msg.directionForMessage + direction) / 2;
        }
        // printUnit("processing message", msg.transform.position.ToString());
    }

    // Start is called before the first frame update
    protected override void Started()
    {
        body = GetComponent<Rigidbody>();
        // ctx = NetworkScene.Register(this);
        // NetworkScene.FindNetworkScene(this);
        LE = new ExperimentLogEmitter(this);
        LM = networkScene.GetComponent<LatencyMeter>();
        timeRecord = 0.1f;
        speed = 0;
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
        timeRecord += Time.deltaTime;
        if (owner)
        {
            //for local ownership

            /*
             * Not sure whether I should initialize remote
            if (firstFrame)
            {
                // send message to intialize remote
                SendJson(new Message(transform, speed, direction));
                LE.Log("CubeScript", "Send Message due to initialization", 
                    this.transform.position, owner, networkId, networkScene.GetComponent<RoomClient>().Me.networkId);
                firstFrame = false;
                return;
            }
            */

            if (timeRecord >= 0.1f)
            {
                // update speed and remote position 10 times per second
                speed = Vector3.Distance(this.transform.position, beforePosition);
                direction = (this.transform.position - beforePosition).normalized;
                remotePosition += remoteDirection * remoteSpeed;
            }

            // update remote position

            // in three cases we will send packets to remote ownership
            // first, the difference value between local and remote speed exceed the threshold for speed; 
            // second, the distance between local and remote position exceed the threshold for position;
            // third, the change of rotation exceed the threshold for rotation
            if (Mathf.Abs(speed - remoteSpeed) > epilsonForSpeed 
                || Vector3.Distance(this.transform.position, remotePosition) > epilsonForPosition 
                || Quaternion.Angle(transform.rotation, remoteRotation) > epilsonForRotation)
            {
                SendJson(new Message(transform, speed, direction));
                // printUnit("sending message because the error is larger the threshold", transform.position.ToString());

                if (Vector3.Distance(remotePosition,transform.position) > epilsonForBlendingDistance)
                {
                    // if the distance is too large, we don't blend them
                    remoteSpeed = speed;
                    remoteDirection = direction;
                    remoteRotation = transform.rotation;
                    remotePosition = transform.position;
                }
                else 
                {
                    // blending
                    remoteSpeed = (remoteSpeed + speed) / 2;
                    remoteDirection = (remoteDirection + direction) / 2;
                    remoteRotation = transform.rotation;
                }
                LE.Log("Send Message due to error exceed threshold");
                // update remote speed and direction
            }
        }
        else
        {
            // for remote ownership

            // disable the physcial simulation if the ownership is remote
            body.isKinematic = true;

            // update the position based on received speed and direction

            if (timeRecord >= 0.1f)
            {
                this.transform.position = this.transform.position + direction * speed;
            }
            // printUnit("update in remote", transform.position.ToString());
        }

        LE.Log("CubeScript", this.transform.position, owner, networkId);

        // Logging Latency
        // reset timerecord
        if (timeRecord >= 0.1f)
        {
            timeRecord = 0;
        }

        // update the position of last frame to compute the moving speed and direction, and rotation to check if there is a change of rotation
        beforePosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);
    }

    protected override void OnSpawn(bool local)
    {
        owner = local;
    }

    private void printUnit(string info, string printPosition)
    {
        /* This function is to print the status of block
         * Input Varible: info and position, the type of both is string
         * Then it will print the information of position, Kinematic, speed and direction
         */
        Debug.Log("---------" + info + "---------");
        Debug.Log("position is " + printPosition);
        Debug.Log("isKinematic is " + body.isKinematic.ToString());
        Debug.Log("speed is " + speed.ToString());
        Debug.Log("direction is " + direction.ToString());
    }
}
