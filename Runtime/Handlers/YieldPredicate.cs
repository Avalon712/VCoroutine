using System;

namespace CysharpCoroutine.Handlers
{
    public sealed class YieldPredicate : YieldHandler
    {
        public override Type YieldType => typeof(Func<bool>);
        
        protected override bool HandleYield(VCoroutine.CoroutineRecorder recorder)
        {
            Func<bool> predicate = (Func<bool>)recorder.Yield;
            return predicate.Invoke();
        }
    }
}