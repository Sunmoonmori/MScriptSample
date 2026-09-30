using System.Linq;
using SampleScene.SampleCode.Script;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

namespace SampleScene.SampleCode.UI
{
    public class MonoSampleUI : MonoBehaviour
    {
        private PanelRenderer _panelRenderer;

        private Button _connectionButton;
        private Label _connectionLabel;
        private DropdownField _dropdown;
        private Label _label;
        private Button _submitButton;

        private string _selected;

        private void OnEnable()
        {
            _panelRenderer = GetComponent<PanelRenderer>();
            _panelRenderer.RegisterUIReloadCallback(OnUIReload);
            ScriptManager.GlobalData.UIObject = this;
        }

        private void OnDisable()
        {
            ScriptManager.GlobalData.UIObject = null;

            _selected = null;
            
            _panelRenderer.UnregisterUIReloadCallback(OnUIReload);
            _panelRenderer = null;

            _dropdown?.UnregisterValueChangedCallback(OnDropdownChanged);
            _submitButton?.UnregisterCallback<ClickEvent>(OnClickEvent);
            _connectionButton?.UnregisterCallback<ClickEvent>(ToggleConnection);
            _dropdown = null;
            _label = null;
            _submitButton = null;
            _connectionLabel = null;
            _connectionButton = null;
        }

        private void Update()
        {
            if (_connectionLabel != null)
            {
                _connectionLabel.text = Instance.Client.Connected ? "Connected" : "Disconnected";
            }
        }
        
        private void OnUIReload(PanelRenderer renderer, VisualElement root)
        {
            _dropdown?.UnregisterValueChangedCallback(OnDropdownChanged);
            _submitButton?.UnregisterCallback<ClickEvent>(OnClickEvent);

            _connectionButton = root.Q<Button>("toggleConnection");
            _connectionLabel = root.Q<Label>("connectionStatus");
            _dropdown = root.Q<DropdownField>("scriptInstancesDropdown");
            _label = root.Q<Label>("messageContentLabel");
            _submitButton = root.Q<Button>("callEnterButton");

            if (_connectionButton == null || _connectionLabel == null || _dropdown == null || _label == null ||
                _submitButton == null)
            {
                Debug.LogError("ui elements lost");
                return;
            }

            _connectionButton.RegisterCallback<ClickEvent>(ToggleConnection);
            _connectionLabel.text = Instance.Client.Connected ? "Connected" : "Disconnected";
            
            var choices = Instance.ScriptManager.Scripts.Keys.ToList();
            _dropdown.choices = choices;
            _dropdown.RegisterValueChangedCallback(OnDropdownChanged);
            _submitButton.RegisterCallback<ClickEvent>(OnClickEvent);
        }

        private void OnDropdownChanged(ChangeEvent<string> evt)
        {
            _selected = evt.newValue;
        }

        private void OnClickEvent(ClickEvent clickEvent)
        {
            if (!Instance.ScriptManager.Scripts.TryGetValue(_selected, out var scriptWrapper))
            {
                return;
            }

            Profiler.BeginSample("CallEnter");
            scriptWrapper.CallEnter();
            Profiler.EndSample();
        }

        private static void ToggleConnection(ClickEvent clickEvent)
        {
            if (Instance.Client.Connected)
            {
                Instance.Client.Close();
            }
            else
            {
                Instance.Client.Connect();
            }
        }
        
        public void SetMessage(string msg)
        {
            msg ??= string.Empty;
            _label.text = msg;
        }
    }
}
