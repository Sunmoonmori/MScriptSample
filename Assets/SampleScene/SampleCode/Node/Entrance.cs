using MScript;

namespace SampleScene.SampleCode.Node
{
    [EntranceNode(Name = "Enter")]
    public static class Enter
    {
        [FlowIn(
            Name = "Do",
            FlowOut = new[] { "Out" })]
        public static int Test(MScriptWrapper.Enter.DoContext ctx)
        {
            return ctx.GotoOut();
        }
    }
}