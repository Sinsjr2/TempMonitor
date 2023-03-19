using System;
using System.Threading;
using TEA;
using TempMonitor.Messages.TemperatureAndHumidityMonitor;
using TempMonitor.Models;
using UnitsNet;

namespace TempMonitor.Messages.TemperatureAndHumidityMonitor {

    public interface ITemperatureAndHumidityMonitorMessage {}

    public record OnChangedIntervalTime(TimeSpan IntervalTime) : ITemperatureAndHumidityMonitorMessage;

    public record OnChangedStatus(TemperatureAndHumidityMonitorStatus Status) : ITemperatureAndHumidityMonitorMessage;

    public record OnReadTempAndHumidity(
        Temperature? temperature,
        RelativeHumidity? humidity) : ITemperatureAndHumidityMonitorMessage;

}

namespace TempMonitor.Models {

    public enum TemperatureAndHumidityMonitorStatus {
        Stop,
        Running,
    }

    public record TemperatureAndHumidityMonitorModel(
        TemperatureAndHumidityMonitorStatus Status,
        TimeSpan IntervalTime
    ) : IUpdate<TemperatureAndHumidityMonitorModel, ITemperatureAndHumidityMonitorMessage> {

        public TimeSpan TimeForTimer => Status switch {
            TemperatureAndHumidityMonitorStatus.Running => IntervalTime,
            TemperatureAndHumidityMonitorStatus.Stop => Timeout.InfiniteTimeSpan,
        };

        public TemperatureAndHumidityMonitorModel Update(ITemperatureAndHumidityMonitorMessage message) {
            return message switch {
                OnChangedIntervalTime(var interval) => this with { IntervalTime = interval },
                OnChangedStatus(var status) => this with { Status = status },
                _ => this
            };
        }

        public static readonly TemperatureAndHumidityMonitorModel Default = new(
            TemperatureAndHumidityMonitorStatus.Stop,
            Timeout.InfiniteTimeSpan
        );
    }
}

namespace TempMonitor {

    public interface ITempAndHumidityReader {
        bool TryReadHumidity (out RelativeHumidity humidity);
        bool TryReadTemperature(out Temperature temperature);
    }

    public class TemperatureAndHumidityMonitor : IRender<TemperatureAndHumidityMonitorModel> {

        readonly Timer timer;

        /// <summary>
        /// 指定した周期で温湿度を監視し、通知します。
        /// </summary>
        public TemperatureAndHumidityMonitor(IDispatcher<OnReadTempAndHumidity> dispatcher,
                                             ITempAndHumidityReader reader) {
            timer = new Timer(_ => { ReadTempAndHumidity(dispatcher, reader); }, null,
                              Timeout.InfiniteTimeSpan,
                              Timeout.InfiniteTimeSpan);
        }

        static void ReadTempAndHumidity(IDispatcher<OnReadTempAndHumidity> dispatcher,
                                        ITempAndHumidityReader reader) {
            var temp = reader.TryReadTemperature(out var t) ? t : default(Temperature?);
            var humidity = reader.TryReadHumidity(out var h) ? h : default(RelativeHumidity?);
            dispatcher.Dispatch(new OnReadTempAndHumidity(temp, humidity));
        }

        public void Render(TemperatureAndHumidityMonitorModel state) {
            timer.Change(state.TimeForTimer, state.TimeForTimer);
        }
    }
}
