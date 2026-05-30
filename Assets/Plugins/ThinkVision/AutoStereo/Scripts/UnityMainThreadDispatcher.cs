using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace AS3DPlugin
{
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        public bool EnableDispather { get; set;}
        private object dispatherLock = new object();
        private static readonly Queue<Action> _executionQueue = new Queue<Action>();
        public void Update()
        {
            lock (dispatherLock)
            {
                if (EnableDispather)
                {
                    lock (_executionQueue)
                    {
                        while (_executionQueue.Count > 0)
                        {
                            _executionQueue.Dequeue().Invoke();
                        }
                    }
                }
            }
        }
        public void Enqueue(IEnumerator action)
        {
            lock (_executionQueue)
            {
                _executionQueue.Enqueue(() =>
                {
                    StartCoroutine(action);
                });
            }
        }
        public void Enqueue(Action action)
        {
            Enqueue(ActionWrapper(action));
        }
        IEnumerator ActionWrapper(Action a)
        {
            a();
            yield return null;
        }


        private static UnityMainThreadDispatcher _instance = null;

        public static bool Exists()
        {
            return _instance != null;
        }

        public static UnityMainThreadDispatcher Instance()
        {
            if (!Exists())
            {
                return null;
            }
            return _instance;
        }


        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
        }
        private void OnApplicationQuit()
        {
            _instance = null;
        }
    }
}