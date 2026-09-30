using System.Diagnostics;
using MScript;
using SampleScene.SampleCode.Script;
using SampleScene.SampleCode.UI;
using Debug = UnityEngine.Debug;

namespace SampleScene.SampleCode.Node
{
    [ProcedureNode(Name = "StopWatch")]
    public class StopWatch
    {
        private Stopwatch _stopwatch;

        [Initialize]
        public void Init(MScriptWrapper.StopWatch.Context _)
        {
            _stopwatch = new Stopwatch();
        }

        [Destroy]
        public void Final(MScriptWrapper.StopWatch.Context _)
        {
            _stopwatch.Reset();
            _stopwatch = null;
        }

        [FlowIn(
            Name = "Start",
            FlowOut = new[] { "StartOut" })]
        public int Start(MScriptWrapper.StopWatch.StartContext ctx)
        {
            _stopwatch!.Start();
            return ctx.GotoStartOut();
        }

        [FlowIn(
            Name = "Stop",
            FlowOut = new[] { "StopOut" },
            ValueOut = new[] { "Elapsed" },
            ValueOutType = new[] { typeof(double) })]
        public int Stop(MScriptWrapper.StopWatch.StopContext ctx)
        {
            _stopwatch!.Stop();
            ctx.SetElapsed(_stopwatch!.Elapsed.TotalSeconds);
            _stopwatch!.Reset();
            return ctx.GotoStopOut();
        }
    }

    [ProcedureNode(Name = "DebugLog")]
    public static class DebugLog
    {
        [FlowIn(
            Name = "In",
            ValueIn = new[] { "Value" },
            ValueInType = new[] { typeof(string) },
            FlowOut = new[] { "Out" })]
        public static int In(MScriptWrapper.DebugLog.InContext ctx)
        {
            Debug.Log(ctx.GetValue());
            return ctx.GotoOut();
        }
    }
    
    [FunctionNode(Name = "GlobalUIObject")]
    public class GlobalUIObject
    {
        [FlowIn(
            Name = "In",
            ValueOut = new[] { "UIObject" },
            ValueOutType = new[] { typeof(MonoSampleUI) })]
        public static int In(MScriptWrapper.GlobalUIObject.InContext ctx)
        {
            var globalVariable = (ScriptGlobalVariables)ctx.GetGlobalVariableRoot();
            ctx.SetUIObject(globalVariable.GlobalData.UIObject);
            return ctx.Discard();
        }
    }
    
    [ProcedureNode(Name = "UIMessageLog")]
    public static class Log
    {
        [FlowIn(
            Name = "In",
            ValueIn = new[] { "UIObject", "Value" },
            ValueInType = new[] { typeof(MonoSampleUI), typeof(string) },
            FlowOut = new[] { "Out" })]
        public static int In(MScriptWrapper.UIMessageLog.InContext ctx)
        {
            ctx.GetUIObject()?.SetMessage(ctx.GetValue());
            return ctx.GotoOut();
        }
    }
}