using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ubiq.XR;
using UnityEngine;
using Ubiq;
using Ubiq.Spawning;
using Ubiq.Logging;

namespace Ubiq.Samples
{
    public interface FallingCube
    {
        void Attach(Hand hand);
    }
    public class boxGenerator : MonoBehaviour, IUseable
    {
        // public GameObject FireworkPrefab;

        private Hand follow;
        public GameObject FallingPrefab;
        private LogEmitter start;

        public void UnUse(Hand controller)
        {
        }

        public void Use(Hand controller)
        {
            for (int i=0;i<10;i++)
            {
                var cube = NetworkSpawnManager.Find(this).SpawnWithPeerScope(FallingPrefab);
                cube.transform.position = new Vector3(10+i*2, 10, 0);
            }
            //cube.transform.position = ;
            start.Log("start");

        }

        // private Rigidbody body;

        /*
        public void Use(Hand controller)
        {
            var firework = NetworkSpawner.SpawnPersistent(this, FireworkPrefab).GetComponents<MonoBehaviour>().Where(mb => mb is IFirework).FirstOrDefault() as IFirework;
            if (firework != null)
            {
                firework.Attach(controller);
            }
        }
        */

        // Start is called before the first frame update
        void Start()
        {
            start = new ExperimentLogEmitter(this);
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
