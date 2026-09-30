using MScript;

namespace SampleScene.SampleCode.Node
{
    public class IntValue
    {
        public int Value;
    }
    
    [FunctionNode(Name = "IntValue")]
    public class IntValueNode
    {
        private IntValue value = new();
        
        [FlowIn(
            Name = "In",
            ValueOut = new[] { "Target" },
            ValueOutType = new[] { typeof(IntValue) })]
        public int In(MScriptWrapper.IntValue.InContext ctx)
        {
            ctx.SetTarget(value);
            return ctx.Discard();
        }
    }
    
    [ProcedureNode(Name = "GetIntValue")]
    public class GetIntValue
    {
        [FlowIn(
            Name = "In",
            ValueIn = new[] { "Target" },
            ValueInType = new[] { typeof(IntValue) },
            ValueOut = new[] { "Value" },
            ValueOutType = new[] { typeof(int) },
            FlowOut = new[] { "Out" })]
        public int GetValue(MScriptWrapper.GetIntValue.InContext ctx)
        {
            ctx.SetValue(ctx.GetTarget()?.Value ?? 0);
            return ctx.GotoOut();
        }
    }

    [ProcedureNode(Name = "SetIntValue")]
    public class SetIntValue
    {
        [FlowIn(
            Name = "In",
            ValueIn = new[] { "Target", "Value" },
            ValueInType = new[] { typeof(IntValue), typeof(int) },
            FlowOut = new[] { "Out" })]
        public int SetValue(MScriptWrapper.SetIntValue.InContext ctx)
        {
            var target = ctx.GetTarget();
            if (target != null)
            {
                target.Value = ctx.GetValue();
            }
            return ctx.GotoOut();
        }
    }

    [ProcedureNode(Name = "IncreaseInt")]
    public static class IncreaseInt
    {
        [FlowIn(
            Name = "In",
            ValueIn = new[] { "Value" },
            ValueInType = new[] { typeof(int) },
            ValueOut = new[] { "Value" },
            ValueOutType = new[] { typeof(int) },
            FlowOut = new[] { "Out" })]
        public static int In(MScriptWrapper.IncreaseInt.InContext ctx)
        {
            ctx.SetValue(ctx.GetValue() + 1);
            return ctx.GotoOut();
        }
    }

    [FunctionNode(Name = "BiggerThanInt")]
    public static class BiggerThanInt
    {
        [FlowIn(
            Name = "In",
            ValueIn = new[] { "Value1", "Value2" },
            ValueInType = new[] { typeof(int), typeof(int) },
            ValueOut = new[] { "Result" },
            ValueOutType = new[] { typeof(bool) })]
        public static int In(MScriptWrapper.BiggerThanInt.InContext ctx)
        {
            ctx.SetResult(ctx.GetValue1() > ctx.GetValue2());
            return ctx.Discard();
        }
    }
}