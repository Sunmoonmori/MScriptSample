using System;
using Grpc.Net.Client;
using MScriptDevCli.Client;
using MScriptWrapperRegistry;
using SampleScene.SampleCode.Script;
using UnityEngine;
using UnityEngine.Networking;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SampleScene.SampleCode.Client
{
    public class Client
    {
        private readonly ScriptManager _scriptManager;
        
        private volatile MScriptDevCli.Client.MScriptDevCli _client;
        private volatile bool _connected;
        public bool Connected => _connected;

        public Client(ScriptManager scriptManager)
        {
            _scriptManager = scriptManager;
        }

        private static void LogException(Exception ex)
        {
            Debug.LogException(ex);
        }
        
        public void Connect()
        {
            if (Connected)
            {
                return;
            }

            var builder = new MScriptDevCliBuilder
            {
                NodeQuerier = NodeImageRegistry.GetNodeImage,
                PortTypeQuerier = TypeInfoRegistry.GetPortTypeInfo,
                AllEditorNodeInfoQuerier = NodeInfoRegistry.GetAllNodeInfo,
                EditorNodeInfoQuerier = NodeInfoRegistry.GetNodeInfo,
                EditorSerializer = _scriptManager.EditorSerializer,
                Registry = _scriptManager.Registry,
                LogException = LogException,
            };
            _client = builder.Create();
            var clientConnection = _client.Connect(
                channelOptions: new GrpcChannelOptions
                {
                    HttpHandler = new UnityHttpMessageHandler
                    {
                        HttpForcedVersion = HttpForcedVersion.HTTP2,
                    }
                });
            _connected = true;
            clientConnection.ContinueWith(_ =>
            {
                _client = null;
                _connected = false;
#if UNITY_EDITOR
                AssemblyReloadEvents.beforeAssemblyReload -= Close;
#endif
            });
#if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload += Close;
#endif
        }

        public void Close()
        {
            if (_client == null)
            {
                return;
            }

            // the http 2.0 provided by unity 6.5 has bug
            // if the client receives nothing before the server closes
            // the client reading stream will never close and cannot be canceled
            // we use force close to kill the connection directly
            _client.ForceClose();
            _client = null;
        }
    }
}