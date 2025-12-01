using System;

namespace VC.Handlers
{
    public sealed class YieldCoroutineID : YieldHandler
    {
        public override Type YieldType => typeof(CoroutineID);
        
        protected override bool HandleYield(VCoroutine.CoroutineRecorder recorder)
        {
            CoroutineID dependency = (CoroutineID)recorder.Yield;
            VCoroutine.CombineDependency(recorder.CoroutineId, dependency.id);
            return true;
        }
    }
}