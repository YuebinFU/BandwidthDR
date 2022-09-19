using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Samples;
using Ubiq;
using Ubiq.Logging;
using Ubiq.Rooms;

public class Baseline : NetworkBehaviour
{
    private LogEmitter LE;
    public bool owner = true;
    float timeR = 0.0f;

    public struct Message
    {
        public TransformMessage transform;
        public Message(Transform transform)
        {
            this.transform = new TransformMessage(transform);
        }
    }

    protected override void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<Message>();
        transform.localPosition = msg.transform.position;
        transform.localRotation = msg.transform.rotation;
    }

    // Start is called before the first frame update
    protected override void Started()
    {
        LE = new ExperimentLogEmitter(this);
    }

    // Update is called once per frame
    void Update()
    {
        if (owner)
        {
            timeR += Time.deltaTime;
            if (timeR > 1/50)
            {
                timeR = 0;
                var mes = new Message(transform);
                LE.Log("Send Message due to error exceed threshold", System.Runtime.InteropServices.Marshal.SizeOf(mes));
                SendJson(new Message(transform));
            }
        }
        LE.Log("CubeScript", this.transform.position.x, this.transform.position.y, this.transform.position.z, owner, networkId);
    }

    protected override void OnSpawn(bool local)
    {
        owner = local;
    }
}
