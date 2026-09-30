using System.Collections;
using System.Collections.Generic;
using MScript;
using SampleScene.SampleCode.Script;
using UnityEngine;

namespace SampleScene.SampleCode.Node
{
    [ProcedureNode(Name = "If")]
    public static class If
    {
        [FlowIn(
            Name = "In",
            ValueIn = new[] { "Value" },
            ValueInType = new[] { typeof(bool) },
            FlowOut = new[] { "True", "False" })]
        public static int In(MScriptWrapper.If.InContext ctx)
        {
            return ctx.GetValue() ? ctx.GotoTrue() : ctx.GotoFalse();
        }
    }

    [ProcedureNode(Name = "Sequence")]
    public static class Sequence
    {
        [FlowIn(
            Name = "In",
            FlowOut = new[] { "First", "Second" })]
        public static IEnumerable<int> In(MScriptWrapper.Sequence.InContext ctx)
        {
            yield return ctx.GotoFirst();
            yield return ctx.GotoSecond();
        }
    }

    [ProcedureNode(Name = "Merge")]
    public class Merge
    {
        private bool _in1Called;
        private bool _in2Called;

        [FlowIn(
            Name = "In1",
            FlowOut = new[] { "Out" })]
        public int In1(MScriptWrapper.Merge.In1Context ctx)
        {
            if (_in2Called)
            {
                _in2Called = false;
                return ctx.GotoOut();
            }

            _in1Called = true;
            return ctx.Discard();
        }

        [FlowIn(
            Name = "In2",
            FlowOut = new[] { "Out" })]
        public int In2(MScriptWrapper.Merge.In2Context ctx)
        {
            if (_in1Called)
            {
                _in1Called = false;
                return ctx.GotoOut();
            }

            _in2Called = true;
            return ctx.Discard();
        }
    }

    [ProcedureNode(Name = "WaitSeconds")]
    public class WaitSeconds
    {
        private readonly List<Coroutine> _coroutines = new();
        
        [FlowIn(
            Name = "In",
            ValueIn = new[] { "Seconds" },
            ValueInType = new[] { typeof(float) },
            FlowOut = new[] { "Out" },
            AsyncFlowOut = new[] { "AsyncOut" })]
        public int Set(MScriptWrapper.WaitSeconds.InContext ctx)
        {
            var seconds = ctx.GetSeconds();
            var coroutine = RuntimeInstance.Ins.StartCoroutine(Wait(seconds, ctx));
            _coroutines.Add(coroutine);
            return ctx.GotoOut();
        }

        [Destroy]
        public void Destroy(MScriptWrapper.WaitSeconds.Context _)
        {
            foreach (var coroutine in _coroutines)
            {
                RuntimeInstance.Ins.StopCoroutine(coroutine);
            }
            _coroutines.Clear();
        }

        private static IEnumerator Wait(float seconds, MScriptWrapper.WaitSeconds.InContext ctx)
        {
            do
            {
                yield return null;
            } while ((seconds -= Time.deltaTime) > 0f);
            ctx.CallAsyncOut();
        }
    }
}