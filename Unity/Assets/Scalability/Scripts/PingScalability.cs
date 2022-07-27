using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Rooms;
using System.Diagnostics;
using Ubiq.Logging;

public class PingScalability : MonoBehaviour
{
    RoomClient rc;
    float now;
    float before;
    Stopwatch stopwatch = new Stopwatch();
    private LogEmitter latencies;

    // Start is called before the first frame update
    void Start()
    {
        rc = GetComponent<RoomClient>();
        rc.PingCallback += OnPing;
        now = 0;
        before = 0;
        latencies = new ExperimentLogEmitter(this);
    }

    // Update is called once per frame
    void Update()
    {
        //if (Time.realtimeSinceStartup > 5.0f)
        //{
        //    rc.Ping();
        //}
        now += Time.deltaTime;
        if (now - before >= 1.0f)
        {
            before = now;
            stopwatch.Restart();
            rc.Ping();
        }
    }

    void OnPing()
    {
        stopwatch.Stop();
        // UnityEngine.Debug.Log(stopwatch.ElapsedMilliseconds);
        latencies.Log("ping", stopwatch.ElapsedMilliseconds);
        // just some comments
    }

}
