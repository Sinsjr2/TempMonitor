using System.Collections.Generic;
using TEA;

namespace TempMonitor {
    /// <summary>
    /// 複数のディスパッチャーに対してディスパッチします。
    /// </summary>
    public class BroadcastDispatcher<TMessage> : IDispatcher<TMessage> {
        IReadOnlyList<IDispatcher<TMessage>> dispatchers;

        public BroadcastDispatcher(IReadOnlyList<IDispatcher<TMessage>> dispatchers){
            this.dispatchers = dispatchers;
        }

        public void Dispatch(TMessage msg) {
            foreach (var dispatcher in dispatchers) {
                dispatcher.Dispatch(msg);
            }
        }
    }

}
