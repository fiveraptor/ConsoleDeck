using System.IO.Ports;

namespace ConsoleDeck.Core;

public enum ConnectionState { Disconnected, Searching, Connected }

public class SerialService
{
    private const int BaudRate = 9600;

    public event Action<string>? MessageReceived;
    public event Action<ConnectionState>? StatusChanged;

    public ConnectionState Status { get; private set; } = ConnectionState.Disconnected;

    private SerialPort? _port;
    private CancellationTokenSource? _cts;

    public void Start()
    {
        _cts = new CancellationTokenSource();
        Task.Run(() => RunLoop(_cts.Token));
    }

    public void Stop()
    {
        _cts?.Cancel();
        _port?.Close();
    }

    private async Task RunLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            SetStatus(ConnectionState.Searching);
            var portName = await FindArduinoPort(ct);
            if (portName == null)
            {
                await Task.Delay(5000, ct).ContinueWith(_ => { });
                continue;
            }

            SetStatus(ConnectionState.Connected);
            try
            {
                _port = new SerialPort(portName, BaudRate) { ReadTimeout = 1000 };
                _port.Open();
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        var line = _port.ReadLine().Trim();
                        if (line.Length > 0)
                            MessageReceived?.Invoke(line);
                    }
                    catch (TimeoutException) { }
                }
            }
            catch (Exception)
            {
                _port?.Close();
                _port = null;
                SetStatus(ConnectionState.Disconnected);
                await Task.Delay(5000, ct).ContinueWith(_ => { });
            }
        }
    }

    private async Task<string?> FindArduinoPort(CancellationToken ct)
    {
        var ports = SerialPort.GetPortNames();
        foreach (var name in ports)
        {
            if (ct.IsCancellationRequested) return null;
            try
            {
                using var sp = new SerialPort(name, BaudRate) { ReadTimeout = 3000, WriteTimeout = 1000 };
                sp.Open();
                await Task.Delay(2000, ct).ContinueWith(_ => { }); // wait for Arduino reset
                sp.DiscardInBuffer();
                sp.WriteLine("WHO");
                var response = sp.ReadLine().Trim();
                if (response == "CONSOLEDECK")
                    return name;
            }
            catch { }
        }
        return null;
    }

    private void SetStatus(ConnectionState state)
    {
        Status = state;
        StatusChanged?.Invoke(state);
    }
}
