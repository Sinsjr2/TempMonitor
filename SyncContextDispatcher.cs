using TEA;

namespace TempMonitor {

    /// <summary>
    /// メインループに対してディスパッチします。
    /// </summary>
    public class SyncContextDispatcher<TMessage> : IDispatcher<TMessage> {
        readonly ELooper.ELooper looper;
        readonly IDispatcher<TMessage> dispatcher;

        public SyncContextDispatcher(ELooper.ELooper looper, IDispatcher<TMessage> dispatcher) {
            this.looper = looper;
            this.dispatcher = dispatcher;
        }

        public void Dispatch(TMessage msg) {
            looper.Enqueue(() => dispatcher.Dispatch(msg));
        }
    }

}
