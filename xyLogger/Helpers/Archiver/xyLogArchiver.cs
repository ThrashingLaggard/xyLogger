using xyLogger.Helpers.Formatters;

namespace xyLogger.Helpers.Archiver
{
    public class xyLogArchiver
    {
        private readonly long _maxFileSize = 1000000; // 1MB?!

        public xyLogArchiver(long maxFileSize)
        {
            _maxFileSize = maxFileSize;
        }

        public string FormatForArchive(string filePath)
        {
            string archivePath = $"{filePath}_{DateTimeOffset.Now:yyyyMMdd_HHmmss}.log";
            xyOutput.Output("Formatting for archive: " + archivePath);
            return archivePath;
        }

        public void MoveLogToArchiveFileIfTooBig(string filepath_)
        {
            if (File.Exists(filepath_) && new FileInfo(filepath_).Length > _maxFileSize)
            {
                string newPath = FormatForArchive(filepath_);
                try
                {
                    File.Move(filepath_, newPath);
                    xyOutput.Output("Moving the log to archive was successfull");
                }
                catch (Exception ioEx)
                {
                    xyOutput.Output(xyLogFormatter.FormatExceptionDetails(ioEx));
                }
            }
        }

        public async Task MoveLogToArchiveFileIfTooBigAsync(string filepath_)
        {
            if (File.Exists(filepath_) && new FileInfo(filepath_).Length > _maxFileSize)
            {
                string newPath = "";
                newPath = FormatForArchive(filepath_);
                try
                {
                    await Task.Run(() => File.Move(filepath_, newPath));
                    await xyOutput.OutputAsync("Moving the log to archive was successfull");

                }
                catch (Exception Ex)
                {
                    xyOutput.Output(xyLogFormatter.FormatExceptionDetails(Ex));
                }
            }
        }
    }
}
