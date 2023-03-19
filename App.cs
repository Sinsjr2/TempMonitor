using TEA;
using TempMonitor.Messages.App;
using TempMonitor.Messages.TemperatureAndHumidityMonitor;
using TempMonitor.Models;

namespace TempMonitor.Messages.App {
    public interface IAppMessage {}

    public record MonitorMessageInApp(ITemperatureAndHumidityMonitorMessage msg) : IAppMessage { }
}

namespace TempMonitor.Models {

    public record AppModel(
        TemperatureAndHumidityMonitorModel TempAndHumidity
    ) : IUpdate<AppModel, IAppMessage> {

        public CommandReaderModel CommandReader => new(true, this);

        public AppModel Update(IAppMessage message) {
            return message switch {
                MonitorMessageInApp(var msg) => this with { TempAndHumidity = TempAndHumidity.Update(msg) },
                _ => this
            };
        }
    }
}

namespace TempMonitor.Views {

    public class AppRender : IRender<AppModel> {
        readonly CommandReader commandReader;
        readonly TemperatureAndHumidityMonitor tempAndHumidity;

        public AppRender(CommandReader commandReader, TemperatureAndHumidityMonitor tempAndHumidity) {
            this.commandReader = commandReader;
            this.tempAndHumidity = tempAndHumidity;
        }

        public void Render(AppModel state) {
            commandReader.Render(state.CommandReader);
            tempAndHumidity.Render(state.TempAndHumidity);
        }
    }
}
