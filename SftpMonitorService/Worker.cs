using SftpMonitorService.Services;
using Microsoft.Extensions.Options;
using SftpMonitorService.TransferLog;

namespace SftpMonitorService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private FileSystemWatcher? _folderWatcher;
        private readonly string _inputFolder;
        private readonly IServiceProvider _services;
        private readonly IHandler _logFileHandler;

        public Worker(ILogger<Worker> logger, IOptions<AppSettings> settings, IServiceProvider services, IHandler logFileHandler)
        {
            _logger = logger;
            _services = services;
            _inputFolder = settings.Value.InputFolder;
            _logFileHandler = logFileHandler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.CompletedTask;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Service Starting");
            if (!Directory.Exists(_inputFolder))
            {
                _logger.LogWarning("Please make sure the InputFolder [{inputFolder}] exists, then restart the service.", _inputFolder);
                return Task.CompletedTask;
            }

            _logger.LogInformation("Binding Events from Input Folder: {inputFolder}", _inputFolder);
            _folderWatcher = new FileSystemWatcher(_inputFolder, "*.ZIP")
            {
                NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.FileName |
                                  NotifyFilters.DirectoryName
            };
            _folderWatcher.Created += Input_OnChanged;
            _folderWatcher.EnableRaisingEvents = true;

            return base.StartAsync(cancellationToken);
        }

        protected async void Input_OnChanged(object source, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Created)
            {
                string logMessage = $"InBound Change Event Triggered by [{e.FullPath}]";
                _logger.LogInformation(logMessage, e.FullPath);
                _logFileHandler.Insert(logMessage);

                using (var scope = _services.CreateScope())
                {
                    //var ftpService = scope.ServiceProvider.GetRequiredService<IFtpService>();
                    //await ftpService.Run(e);

                    var sFtpService = scope.ServiceProvider.GetRequiredService<ISftpService>();
                    await sFtpService.Run(e);
                }

                logMessage = $"InBound Change Event Completed for [{e.FullPath}]";
                _logger.LogInformation(logMessage, e.FullPath);
                _logFileHandler.Insert(logMessage);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Service");
            if (_folderWatcher != null)
            {
                _folderWatcher.EnableRaisingEvents = false;
            }
            await base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _logger.LogInformation("Disposing Service");
            _folderWatcher?.Dispose();
            base.Dispose();
        }
    }
}
