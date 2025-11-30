using System;
using System.Collections;

namespace CysharpCoroutine.Handlers
{
    public sealed class YieldEnumerator : YieldHandler
    {
        public override Type YieldType => typeof(IEnumerator);
        
        protected override bool HandleYield(VCoroutine.CoroutineRecorder recorder)
        {
            recorder.stack.Push((IEnumerator)recorder.Yield);
            return true;
        }
    }
}