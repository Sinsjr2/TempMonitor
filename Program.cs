using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TEA;
using TempMonitor.Messages.App;
using TempMonitor.Messages.TemperatureAndHumidityMonitor;
using TempMonitor.Models;
using TempMonitor.Views;
using UnitsNet;

namespace TempMonitor {

    internal class NoneRender<TState> : IRender<TState> {
        public void Render(TState state) {
        }
    }

    public class RandomTempAndHumidityReader : ITempAndHumidityReader {

        readonly Random random = new();

        public bool TryReadHumidity(out RelativeHumidity humidity) {
            humidity = RelativeHumidity.FromPercent(random.NextDouble());
            return true;
        }

        public bool TryReadTemperature(out Temperature temperature) {
            var min = 5;
            var max = 34;
            temperature = Temperature.FromDegreesCelsius(min + (max - min) * random.NextDouble());
            return true;
        }
    }

    /// <summary>
    /// ディスパッチされたメッセージをコンソールに出力します。
    /// </summary>
    public class MessageToConsole<TMessage> : IDispatcher<TMessage> {
        public void Dispatch(TMessage msg) {
            Console.WriteLine(msg?.ToString() ?? "null");
        }
    }

    public record CommandReaderModel(
        bool Running,
        AppModel State
    );

    /// <summary>
    /// jsonで読み出したメッセージをデシリアライズして通知します。
    /// </summary>
    public class CommandReader : ConsoleAppBase {
        Stream input;
        IDispatcher<IAppMessage> dispatcher;
        ELooper.ELooper looper;
        CommandReaderModel? prevState = null;

        CancellationTokenSource cancelSource = new();

        public CommandReader(Stream input, IDispatcher<IAppMessage> dispatcher, ELooper.ELooper looper) {
            this.input = input;
            this.dispatcher = dispatcher;
            this.looper = looper;
        }

        public void Render(CommandReaderModel state) {
            var prev = prevState;
            prevState = state;

            if (prev is null || !prev.Running && state.Running) {
                looper.EnqueueTask(() => ReadLoop(cancelSource.Token));
            }
        }

        public void SetMonitorInterval(double ms) {
            dispatcher.Dispatch(
                new MonitorMessageInApp(
                    new OnChangedIntervalTime(TimeSpan.FromMilliseconds(ms))));
        }

        public void SetMonitorStatus(TemperatureAndHumidityMonitorStatus status) {
            dispatcher.Dispatch(
                new MonitorMessageInApp(
                    new OnChangedStatus(status)));
        }

        async Task ReadLoop(CancellationToken token) {
            var reader = new StreamReader(input);
            try {
                while (!token.IsCancellationRequested) {
                    var commandLine = await reader.ReadLineAsync().WaitAsync(token);
                    if (commandLine is null) {
                        return;
                    }
                    if (commandLine is not "") {
                        await ConsoleApp.CreateBuilder(commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                            .Build()
                            .AddCommand("setMonitorInterval", (double ms) => this.SetMonitorInterval(ms))
                            .AddCommand("setMonitorStatus", (TemperatureAndHumidityMonitorStatus status) => SetMonitorStatus(status))
                            .RunAsync();
                    }
                }
            }
            catch (OperationCanceledException) {}
        }
    }

    public class Program : ConsoleAppBase {

        public static void Main(string[] args) {
            ConsoleApp.Run<Program>(args);
        }

        [Command("run")]
        public void Run([Option("input")]string commandReadSourceFilename) {
            var looper = new ELooper.ELooper();
            using var input = new FileStream(Path.GetFileName(commandReadSourceFilename), FileMode.Open);
            var bufDispatcher = new TEA.BufferDispatcher<IAppMessage>();
            var dispatcher = new BroadcastDispatcher<IAppMessage>(
                new IDispatcher<IAppMessage>[] {
                    bufDispatcher,
                        new MessageToConsole<IAppMessage>()
                        });
            var readerForMonitor = new RandomTempAndHumidityReader();
            var tea = new TEA.TEA<AppModel, IAppMessage>(
                new AppModel(Models.TemperatureAndHumidityMonitorModel.Default),
                new AppRender(new CommandReader(input, bufDispatcher, looper),
                              new TemperatureAndHumidityMonitor(
                                  new SyncContextDispatcher<IAppMessage>(looper, dispatcher)
                                  .Wrap((OnReadTempAndHumidity msg) => new MonitorMessageInApp(msg)),
                                  readerForMonitor)
                ));
            bufDispatcher.Setup(tea);

            Context.CancellationToken.Register(() => looper.StopLoop());
            looper.Start();
        }

        [Command("streamTest")]
        public async Task StreamReadTest([Option("input")]string commandReadSourceFilename) {
            using var input = new StreamReader(new FileStream(Path.GetFileName(commandReadSourceFilename), FileMode.Open));

            var token = Context.CancellationToken;
            token.Register(() => Console.WriteLine("キャンセルされました。"));

            Console.WriteLine("ループ開始");
            while (!token.IsCancellationRequested) {
                Console.WriteLine("読み込み開始");
                var line = await input.ReadLineAsync().WaitAsync(token);
                Console.WriteLine("読み込み終了");
                if (line is null) {
                    return;
                }
                Console.WriteLine(line);
            }
            Console.WriteLine("ループ終了");
        }
    }
}
