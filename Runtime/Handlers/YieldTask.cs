using System;
using System.Threading.Tasks;

namespace Csharp.Handlers
{
    public sealed class YieldTask : YieldHandler
    {
        public override Type YieldType => typeof(Task);

        protected override bool HandleYield(VCoroutine.CoroutineRecorder recorder)
        {
            Task task = (Task)recorder.Yield;
            return task.IsCompleted;
        }
    }
}