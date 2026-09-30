using System;
using System.Collections;
using UnityEngine;

namespace SampleScene.SampleCode.Script
{
    public static class Instance
    {
        public static ScriptManager ScriptManager
        {
            get
            {
                if (Application.isPlaying)
                {
                    return RuntimeInstance.Ins.ScriptManager;
                }
                
                return EditorInstance.Ins.ScriptManager;
            }
        }
        public static Client.Client Client
        {
            get
            {
                if (Application.isPlaying)
                {
                    return RuntimeInstance.Ins.Client;
                }
                
                return EditorInstance.Ins.Client;
            }
        }
    }

    public class EditorInstance
    {
        private static EditorInstance _ins;
        public static EditorInstance Ins => _ins ??= new EditorInstance();
        
        public ScriptManager ScriptManager;
        public Client.Client Client;

        public EditorInstance()
        {
            ScriptManager = new ScriptManager();
            ScriptManager.Initialize();
            Client = new Client.Client(ScriptManager);
        }

        ~EditorInstance()
        {
            Client.Close();
            Client = null;
            ScriptManager.Destroy();
            ScriptManager = null;
        }
    }
    
    public class RuntimeInstance : MonoBehaviour
    {
        private static RuntimeInstance _ins;
        public static RuntimeInstance Ins
        {
            get
            {
                if (_ins == null)
                {
                    _ins = new GameObject("CoroutineProvider").AddComponent<RuntimeInstance>();
                }

                return _ins;
            }
        }

        [NonSerialized]
        public ScriptManager ScriptManager;
        [NonSerialized]
        public Client.Client Client;

        private void Awake()
        {
            ScriptManager = new ScriptManager();
            ScriptManager.Initialize();
            Client = new Client.Client(ScriptManager);
        }

        private void OnDestroy()
        {
            Client.Close();
            Client = null;
            ScriptManager.Destroy();
            ScriptManager = null;
        }

        public new Coroutine StartCoroutine(IEnumerator coroutine)
        {
            return base.StartCoroutine(coroutine);
        }

        public new void StopCoroutine(Coroutine coroutine)
        {
            base.StopCoroutine(coroutine);
        }
    }
}