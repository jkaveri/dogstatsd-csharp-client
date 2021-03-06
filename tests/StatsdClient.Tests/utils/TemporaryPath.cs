using System.IO;
using System;

namespace Tests.Utils
{
    /// TemporaryPath returns a path suitable for a temporary file.
    /// The file path will be destroyed when calling Dispose.
    sealed class TemporaryPath : IDisposable
    {
        public TemporaryPath()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                System.IO.Path.GetRandomFileName());
        }

        public string Path { get; }

        public void Dispose()
        {
            File.Delete(Path);
        }
    }
}