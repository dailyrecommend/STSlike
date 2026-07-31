using System;
using STSlike.Domain.Progression;

namespace STSlike.Infrastructure.Persistence
{
    /// <summary>
    /// Volatile <see cref="IRunSaveRepository"/>. Resuming only survives within
    /// the session, which is enough for tests and for running without a database.
    /// </summary>
    public sealed class InMemoryRunSaveRepository : IRunSaveRepository
    {
        private RunSaveData _save;

        public bool HasSave => _save != null;

        public void Save(RunSaveData data)
        {
            _save = data ?? throw new ArgumentNullException(nameof(data));
        }

        public RunSaveData Load() => _save;

        public void Clear() => _save = null;
    }
}
