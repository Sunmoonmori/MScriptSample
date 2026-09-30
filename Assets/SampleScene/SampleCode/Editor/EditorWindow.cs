#if UNITY_EDITOR

using SampleScene.SampleCode.Script;
using UnityEditor;
using UnityEngine;

namespace SampleScene.SampleCode.Editor
{
    public class ConnectionWindow : EditorWindow
    {
        private const int IndicatorTextureSize = 32;
        private const float IndicatorDisplaySize = 20f;

        private static readonly Color ConnectedColor = new Color(0.20f, 0.85f, 0.35f);
        private static readonly Color DisconnectedColor = new Color(0.85f, 0.25f, 0.25f);

        private static bool IsConnected => Instance.Client.Connected;

        private Texture2D _circleTexture;
        private GUIStyle _indicatorStyle;
        private GUIStyle _statusStyle;

        [MenuItem("Tools/MScript Connection Window")]
        private static void Open()
        {
            var window = GetWindow<ConnectionWindow>();
            window.titleContent = new GUIContent("Connection");
            window.minSize = new Vector2(260f, 130f);
            window.Show();
        }

        private void OnEnable()
        {
            if (_circleTexture == null)
                _circleTexture = CreateCircleTexture(IndicatorTextureSize);
        }

        private void OnDisable()
        {
            ReleaseTexture();
        }

        private void OnDestroy()
        {
            ReleaseTexture();
        }

        private static Texture2D CreateCircleTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color[size * size];
            var radius = size * 0.5f;
            var center = new Vector2(radius, radius);

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    var alpha = Mathf.Clamp01(radius - distance);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
        
        private void ReleaseTexture()
        {
            if (_circleTexture != null)
            {
                DestroyImmediate(_circleTexture);
                _circleTexture = null;
            }
        }

        private void OnGUI()
        {
            EnsureStyles();

            EditorGUILayout.Space(12f);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                var prevColor = GUI.color;
                GUI.color = IsConnected ? ConnectedColor : DisconnectedColor;
                GUILayout.Label(
                    _circleTexture,
                    _indicatorStyle,
                    GUILayout.Width(IndicatorDisplaySize),
                    GUILayout.Height(IndicatorDisplaySize));
                GUI.color = prevColor;

                GUILayout.Space(8f);
                GUILayout.Label(
                    IsConnected ? "connected" : "disconnected",
                    _statusStyle,
                    GUILayout.Height(IndicatorDisplaySize));

                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.Space(14f);

            var lineRect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(lineRect, new Color(0f, 0f, 0f, 0.2f));

            EditorGUILayout.Space(14f);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(IsConnected))
                {
                    if (GUILayout.Button("connect", GUILayout.Width(100f), GUILayout.Height(26f)))
                        Connect();
                }

                GUILayout.Space(10f);

                using (new EditorGUI.DisabledScope(!IsConnected))
                {
                    if (GUILayout.Button("close", GUILayout.Width(100f), GUILayout.Height(26f)))
                        Disconnect();
                }

                GUILayout.FlexibleSpace();
            }
        }

        private void EnsureStyles()
        {
            _indicatorStyle ??= new GUIStyle(GUIStyle.none)
            {
                alignment = TextAnchor.MiddleCenter,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0)
            };

            _statusStyle ??= new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12
            };
        }

        private void Connect()
        {
            if (IsConnected)
            {
                return;
            }
            
            Instance.Client.Connect();
            
            Repaint();
        }

        private void Disconnect()
        {
            if (!IsConnected)
            {
                return;
            }
            
            Instance.Client.Close();
            
            Repaint();
        }
    }
}

#endif