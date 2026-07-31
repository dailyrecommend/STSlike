#if STSLIKE_SQLITE
using System;
using System.IO;
using SQLite;
using UnityEngine;

namespace STSlike.Infrastructure.Persistence.Sqlite
{
    /// <summary>
    /// Owns the SQLite connection and the schema. One instance per game session;
    /// the repositories share it so they take part in the same transactions and
    /// the file is opened once.
    ///
    /// The file lives in persistentDataPath, which is also the directory Steam
    /// Cloud is pointed at later — the save being a single file is what makes
    /// that straightforward.
    /// </summary>
    public sealed class SqliteDatabase : IDisposable
    {
        public const string DefaultFileName = "stslike.db";

        /// <summary>Bump this whenever the schema changes, and handle the step in Migrate().</summary>
        public const int SchemaVersion = 1;

        public SQLiteConnection Connection { get; }
        public string Path { get; }

        public SqliteDatabase(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Database path must be provided.", nameof(path));

            Path = path;
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path) ?? ".");

            // sqlite-net needs a native provider registered before the first
            // query. Recent sqlite-net-pcl ships the provider package rather
            // than a "batteries" bundle, so register it explicitly — this works
            // with either packaging and is safe to call more than once.
            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlite3());

            Connection = new SQLiteConnection(path,
                SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex);

            Migrate();
        }

        /// <summary>
        /// Brings an existing file up to the current schema. Shipping an update
        /// must never wipe a player's history, and with Steam Cloud a save from
        /// another machine — possibly written by a different build — can appear
        /// at any time, so the file's version is checked rather than assumed.
        ///
        /// SQLite's own user_version field stores it, which avoids a bootstrap
        /// problem: no table is needed to find out which tables exist.
        /// </summary>
        private void Migrate()
        {
            int version = Connection.ExecuteScalar<int>("PRAGMA user_version");

            if (version > SchemaVersion)
            {
                // Written by a newer build. Leave it untouched: guessing at a
                // downgrade is how saves get corrupted.
                Debug.LogWarning(
                    $"[SqliteDatabase] Save schema v{version} is newer than this build (v{SchemaVersion}). " +
                    "Leaving it as-is; some data may be ignored.");
                return;
            }

            // CreateTable is additive and idempotent: it creates what is missing
            // and adds new columns to tables that already exist.
            Connection.CreateTable<RunHistoryRow>();
            Connection.CreateTable<CardStatsRow>();
            Connection.CreateTable<RunSaveRow>();

            if (version < SchemaVersion)
                Connection.Execute($"PRAGMA user_version = {SchemaVersion}");
        }

        public static SqliteDatabase OpenDefault()
        {
            return new SqliteDatabase(System.IO.Path.Combine(Application.persistentDataPath, DefaultFileName));
        }

        public void Dispose() => Connection?.Dispose();
    }
}
#endif
