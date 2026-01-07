using HidSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Bair_Keyboard_thingy.QMK_API
{
    /// <summary>
    /// Manages a QMK raw HID device: automatically connects when the device appears,
    /// exposes a method to send reports and raises an event when a message is received.
    /// Designed to be used from WPF or other code (no UI calls inside).
    /// </summary>
    public sealed class QMK_HID : IDisposable
    {
        private const int ReportLength = 33;

        public readonly int _vendorId;
        public readonly int _productId;

        private HidStream? _stream;
        private readonly object _streamLock = new();
        private CancellationTokenSource? _readerCts;
        private Task? _readerTask;
        public string name;
        public int layer_count;

        /// <summary>
        /// Raised when a report is received from the device. The byte[] contains the raw report bytes (length may be <= 33).
        /// </summary>
        public event EventHandler<byte[]>? MessageReceived;

        /// <summary>
        /// True when currently connected to a HID device.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                lock (_streamLock)
                {
                    return _stream != null && _stream.CanRead && _stream.CanWrite;
                }
            }
        }

        public QMK_HID(int vendorId, int productId, string name, int layer_count)
        {
            _vendorId = vendorId;
            _productId = productId;
            this.name = name;
            this.layer_count = layer_count;

            // Watch for device attach/detach and attempt to connect automatically.
            DeviceList.Local.Changed += OnDeviceListChanged;

            // Try immediate connect if device already present.
            _ = ReconnectAsync();
        }




        private async void OnDeviceListChanged(object? sender, DeviceListChangedEventArgs e)
        {
            try
            {
                await ReconnectAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"QMK_HID: reconnect on device list change failed: {ex}");
            }
        }

        /// <summary>
        /// Attempts to (re)connect to a matching HID device. Safe to call multiple times.
        /// </summary>
        public async Task ReconnectAsync(CancellationToken cancellationToken = default)
        {
            await Task.Run(() =>
            {
                lock (_streamLock)
                {
                    if (_stream != null && _stream.CanRead && _stream.CanWrite) return;

                    CloseStream_NoLock();

                    HidDevice? rawDevice = null;
                    foreach (var dev in DeviceList.Local.GetHidDevices(_vendorId, _productId))
                    {
                        if (dev.GetMaxOutputReportLength() == ReportLength &&
                            dev.GetMaxInputReportLength() == ReportLength)
                        {
                            rawDevice = dev;
                            break;
                        }
                    }

                    if (rawDevice == null) return;

                    try
                    {
                        var stream = rawDevice.Open();
                        stream.ReadTimeout = System.Threading.Timeout.Infinite;
                        stream.WriteTimeout = 2000;

                        _stream = stream;
                        _readerCts = new CancellationTokenSource();
                        var ct = _readerCts.Token;
                        _readerTask = Task.Run(() => ReaderLoop(stream, ct), CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"QMK_HID: open stream failed: {ex}");
                        CloseStream_NoLock();
                    }
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a report to the connected device. Buffer is padded or truncated to 33 bytes.
        /// Throws InvalidOperationException if not connected.
        /// </summary>
        public Task SendAsync(byte[] buffer, CancellationToken cancellationToken = default)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            lock (_streamLock)
            {
                if (_stream == null) { Debug.WriteLine("HID device not connected."); return Task.CompletedTask; }

                var outBuffer = new byte[ReportLength];
                Array.Copy(buffer, 0, outBuffer, 0, Math.Min(buffer.Length, ReportLength));

                try
                {
                    // HidStream.Write is synchronous; keep it simple and synchronous inside lock.
                    _stream.Write(outBuffer);

                    Debug.WriteLine($"QMK_HID: buffer sent: {BitConverter.ToString(outBuffer, 0, 32)}");

                }
                catch (Exception ex)
                {
                    // Close stream and let device watcher attempt reconnect later.
                    Debug.WriteLine($"QMK_HID: write failed: {ex}");
                    CloseStream_NoLock();
                    throw new InvalidOperationException("Failed to write to HID device.", ex);
                }
            }

            return Task.CompletedTask;
        }

        // ReaderLoop: prefer ReadAsync, fall back to Task.Run if not supported
        private async Task ReaderLoop(HidStream stream, CancellationToken ct)
        {
            var inBuffer = new byte[ReportLength];

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    int bytesRead;
                    try
                    {
                        // Try cooperative cancellation
                        bytesRead = await stream.ReadAsync(inBuffer, 0, inBuffer.Length, ct).ConfigureAwait(false);
                    }
                    catch (NotSupportedException)
                    {
                        // HidSharp may not support ReadAsync; fall back to Task.Run
                        bytesRead = await Task.Run(() => stream.Read(inBuffer, 0, inBuffer.Length), CancellationToken.None).ConfigureAwait(false);
                    }

                    if (bytesRead > 0)
                    {
                        var report = new byte[bytesRead];
                        Array.Copy(inBuffer, 0, report, 0, bytesRead);
                        MessageReceived?.Invoke(this, report);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"QMK_HID: reader error: {ex}");
                    lock (_streamLock)
                    {
                        CloseStream_NoLock();
                    }
                    break;
                }
            }
        }



        private void CloseStream_NoLock()
        {
            // caller must hold _streamLock
            try { _readerCts?.Cancel(); } catch { }
            try { _stream?.Close(); } catch { }
            try { _stream?.Dispose(); } catch { }

            _stream = null;
            try { _readerCts?.Dispose(); } catch { }
            _readerCts = null;
        }

        public void Dispose()
        {
            DeviceList.Local.Changed -= OnDeviceListChanged;
            lock (_streamLock)
            {
                CloseStream_NoLock();
            }
            GC.SuppressFinalize(this);
        }
    }
}