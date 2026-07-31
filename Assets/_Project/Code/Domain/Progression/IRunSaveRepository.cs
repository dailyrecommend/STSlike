namespace STSlike.Domain.Progression
{
    /// <summary>
    /// Stores the single in-progress run. There is deliberately only one slot:
    /// a roguelike run is meant to be committed to, and one slot removes any
    /// temptation to save-scum by juggling files.
    /// </summary>
    public interface IRunSaveRepository
    {
        bool HasSave { get; }

        void Save(RunSaveData data);

        /// <summary>Returns null when there is nothing to resume.</summary>
        RunSaveData Load();

        /// <summary>Called once a run ends, so a finished run is never resumable.</summary>
        void Clear();
    }
}
