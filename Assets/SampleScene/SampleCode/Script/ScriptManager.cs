using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using MScriptDevCli.Client;
using SampleScene.SampleCode.UI;
using UnityEditor;
using UnityEngine;

namespace SampleScene.SampleCode.Script
{
    public class GlobalData
    {
        public MonoSampleUI UIObject;
    }
    
    public class ScriptGlobalVariables
    {
        public GlobalData GlobalData;
    }

    public class ScriptWrapper
    {
        public ScriptGlobalVariables ScriptVariables;
        public MScript.MScript Script;
        public Action CallEnter;
    }

    public class ScriptManager
    {
        private SynchronizationContext _mainContext;

        public static readonly GlobalData GlobalData = new();

        private readonly Dictionary<string, ScriptWrapper> _scripts = new();
        public IReadOnlyDictionary<string, ScriptWrapper> Scripts => _scripts;

        public MScriptEditorRegistry Registry { get; private set; }
        public RuntimeSerializer RuntimeSerializer { get; private set; }
        public EditorSerializer EditorSerializer { get; private set; }

        public void Initialize()
        {
            _mainContext = SynchronizationContext.Current;
            Registry = new MScriptEditorRegistry();
            RuntimeSerializer = new RuntimeSerializer(Debug.LogError);
            EditorSerializer = new EditorSerializer();
            var saved = LoadAll();
            if (saved != null)
            {
                var sortSaved = new List<(string, int, byte[])>();
                foreach (var (path, data) in saved)
                {
                    var count = 0;
                    foreach (var c in path)
                    {
                        if (c == '/')
                        {
                            count++;
                        }
                    }
                    sortSaved.Add((path, count, data));
                }
                sortSaved.Sort((a, b) => a.Item2.CompareTo(b.Item2));
                foreach (var (path, _, data) in sortSaved)
                {
                    var image = ConvertImage(data);
                    if (!Registry.ImageRegistry.AddOrUpdate(path, image))
                    {
                        Debug.LogError($"Image {path} was not added successfully");
                    }
                    else if (Application.isPlaying)
                    {
                        CreateNewInstance(path, image);
                    }
                }
            }
            Registry.ImageRegistry.RegisterAddOrUpdateListener(OnImageAdded);
            Registry.ImageRegistry.RegisterRemoveListener(OnImageRemoved);
        }

        public void Destroy()
        {
            Registry.ImageRegistry.UnregisterAddOrUpdateListener(OnImageAdded);
            Registry.ImageRegistry.UnregisterRemoveListener(OnImageRemoved);
            EditorSerializer = null;
            RuntimeSerializer = null;
            Registry = null;
            _mainContext = null;
        }

        public void CreateNewInstance(string path, MScriptImageEntry image)
        {
            MScript.MScript scriptToDestroy = null;
            if (_scripts.TryGetValue(path, out var scriptWrapper))
            {
                scriptToDestroy = scriptWrapper.Script;
            }

            {
                var entrance = image.Image.GetEntranceQuerier("Enter", "Do");
                var globalVariables = new ScriptGlobalVariables
                {
                    GlobalData = GlobalData
                };
                var script = new MScript.MScript(image.Image);
                // initialization must be before the adding to the registry
                script.Initialize(globalVariables, image.SerializedNodeValues, RuntimeSerializer,
                    EditorSerializer);

                _scripts[path] = new ScriptWrapper
                {
                    ScriptVariables = globalVariables,
                    Script = script,
                    CallEnter = () => {
                        foreach (var enter in entrance)
                        {
                            enter.Run(script);
                        }
                    }
                };
                Registry.InstanceRegistry.AddOrUpdate(path,
                    new MScriptInstanceEntry
                    {
                        Script = script,
                        Image = image
                    });
            }

            // destruction must be after the removing from the registry
            scriptToDestroy?.Destroy();
        }

        public void DestroyInstance(string path)
        {
            if (!Registry.InstanceRegistry.TryGet(path, out var instance))
            {
                return;
            }

            Registry.InstanceRegistry.Remove(path);
            _scripts.Remove(path);

            // destruction must be after the removing from the registry
            instance.Script.Destroy();
        }
        
        private void OnImageAdded(string path, MScriptImageEntry value)
        {
            _mainContext.Post(_ =>
            {
                if (Application.isEditor)
                {
                    Save(path, ConvertImage(value));
                }

                if (Application.isPlaying)
                {
                    CreateNewInstance(path, value);
                }
            }, null);
        }

        private void OnImageRemoved(string path)
        {
            _mainContext.Post(_ =>
            {
                if (Application.isEditor)
                {
                    Delete(path);
                }

                if (Application.isPlaying)
                {
                    DestroyInstance(path);
                }
            }, null);
        }

        private MScriptImageEntry ConvertImage(byte[] bytes)
        {
            var s = Encoding.UTF8.GetString(bytes);
            return RuntimeSerializer.Deserialize(s, out MScriptImageEntry entry) ? entry : default;
        }

        private byte[] ConvertImage(MScriptImageEntry entry)
        {
            var s = EditorSerializer.Serialize(entry);
            return Encoding.UTF8.GetBytes(s);
        }

        private static List<(string, byte[])> LoadAll()
        {
            if (!Directory.Exists(Application.streamingAssetsPath))
            {
                return null;
            }
            var files = Directory.GetFiles(Application.streamingAssetsPath, "*.bytes", SearchOption.AllDirectories);
            var result = new List<(string, byte[])>();
            foreach (var f in files)
            {
                var encodedName = Path.GetFileNameWithoutExtension(f);
                var nameNoExt = Encoding.UTF8.GetString(Convert.FromBase64String(encodedName));
                var fileContent = File.ReadAllBytes(f);
                result.Add((nameNoExt, fileContent));
            }
            return result;
        }
        
        private static void Save(string nameNoExt, byte[] bytes)
        {
            if (bytes == null)
            {
                return;
            }
            
            var encodedName = Convert.ToBase64String(Encoding.UTF8.GetBytes(nameNoExt));
            var dir  = Path.Combine(Application.dataPath, "StreamingAssets");
            var path = Path.Combine(dir, $"{encodedName}.bytes");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllBytes(path, bytes);
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
        }
        
        private static void Delete(string nameNoExt)
        {
            var dir  = Path.Combine(Application.dataPath, "StreamingAssets");
            if (!Directory.Exists(dir))
            {
                return;
            }
            var encodedName = Convert.ToBase64String(Encoding.UTF8.GetBytes(nameNoExt));
            var path = Path.Combine(dir, $"{encodedName}.bytes");
            File.Delete(path);
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
        }
    }
}