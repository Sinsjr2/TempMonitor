using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ELooper {

    public class ELooper {

        /// <summary>
        /// メインループから通知されます。
        /// </summary>
        public event Action<Exception>? OnError;
        readonly BlockingCollection<Action> actions = new();

        /// <summary>
        /// スレッドセーフです。
        /// </summary>
        public void Enqueue(Action action) {
            actions.Add(action);
        }

        /// <summary>
        /// スレッドセーフです。
        /// </summary>
        public void EnqueueTask(Func<Task> run) {
            Enqueue(async () => {
                try {
                    await run();
                }
                catch (Exception e) {
                    OnError?.Invoke(e);
                }
            });
        }

        /// <summary>
        /// メインループを実行します。
        /// スレッドセーフではありません。
        /// </summary>
        public void Start() {
            SynchronizationContext? prevContext = null;
            try {
                prevContext = SynchronizationContext.Current;
                SynchronizationContext.SetSynchronizationContext(new ELSynchronizationContext(Enqueue));
                foreach (var action in actions.GetConsumingEnumerable()) {
                    try {
                        action();
                    }
                    catch (Exception e) {
                        OnError?.Invoke(e);
                    }
                }
            }
            finally {
                SynchronizationContext.SetSynchronizationContext(prevContext);
            }
        }

        /// <summary>
        /// メインループを終了する時に呼び出して下さい。
        /// </summary>
        public void StopLoop() {
            actions.CompleteAdding();
        }
    }

    public sealed class ELSynchronizationContext : SynchronizationContext {
        private readonly Action<Action> enqueue;

        public ELSynchronizationContext(Action<Action> enqueue) {
            this.enqueue = enqueue;
        }

        public override void Post(SendOrPostCallback callback, object? state) {
            enqueue(() => callback(state));
        }

        public override void Send(SendOrPostCallback callback, object? state) {
            throw new NotSupportedException();
        }
    }
}
