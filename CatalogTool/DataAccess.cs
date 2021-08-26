using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using Dapper;

namespace CatalogTool
{
    class DataAccess
    {
        private readonly string connStr;

        public DataAccess()
        {
            connStr = GetConnectionString();
        }

        private static string GetConnectionString(string id = "Default")
        {
            return ConfigurationManager.ConnectionStrings[id].ConnectionString;
        }

        public IEnumerable<Catalog> GetCatalogs()
        {
            using (IDbConnection conn = new SQLiteConnection(connStr))
            {
                return conn.Query<Catalog>("select * from Catalogs");
            }
        }

        public IEnumerable<CatalogCount> GetCatalogCounts()
        {
            using (IDbConnection conn = new SQLiteConnection(connStr))
            {
                return conn.Query<CatalogCount>("select [Catalog], count(0) Count from Tracks group by [Catalog]");
            }
        }

        public void AddCatalog(Catalog catalog)
        {
            using (IDbConnection conn = new SQLiteConnection(connStr))
            {
                AddCatalog(catalog, conn);
            }
        }

        public void AddCatalog(Catalog catalog, IDbConnection connection)
        {
            var parameters = new DynamicParameters(new
            {
                catalog.Name
            });

            if (0 == connection.QuerySingle<int>("select count(0) from Catalogs where Name = @Name", parameters))
            {
                connection.Execute("insert into Catalogs (Name) values (@Name)", parameters);
            }
        }

        public IEnumerable<CatalogTrack> GetAllTracks()
        {
            using (IDbConnection conn = new SQLiteConnection(connStr))
            {
                return conn.Query<CatalogTrack>("select * from Tracks", new DynamicParameters());
            }
        }

        public IEnumerable<CatalogTrack> GetCatalogTracks(string catalog)
        {
            using (IDbConnection conn = new SQLiteConnection(connStr))
            {
                var parameters = new DynamicParameters(new { Catalog = catalog });
                return conn.Query<CatalogTrack>("select * from Tracks where [Catalog] = @Catalog", parameters);
            }
        }

        public IEnumerable<CatalogTrack> FindCatalogTracks(string catalog, string[] wordsTrackName, string[] wordsPerformer, string[] wordsComposer, double threashold)
        {
            var wordsCount = wordsTrackName.Length + wordsPerformer.Length + wordsComposer.Length;
            if (wordsCount == 0)
                return Enumerable.Empty<CatalogTrack>();

            var min = Convert.ToUInt32(Math.Floor(wordsCount * threashold));
            var sum = string.Join("+",
                    new[] {
                        GetFieldWordsSum("TrackName", wordsTrackName),
                        GetFieldWordsSum("Performer", wordsPerformer),
                        GetFieldWordsSum("Composer", wordsComposer)
                    }
                    .Where(x => x != null)
                    .ToArray()
                );

            if (string.IsNullOrEmpty(sum))
                return Enumerable.Empty<CatalogTrack>();

            //var sql = $"select * from Tracks where [Catalog] = @Catalog AND {sum} >= {min}";
            var sql = $"select t2.* from (select t.*, {sum} s from Tracks t ) t2 where [Catalog] = @Catalog and t2.s >= {min} order by t2.s desc limit 10";

            using (IDbConnection conn = new SQLiteConnection(connStr))
            {
                var parameters = new DynamicParameters(new { Catalog = catalog });
                return conn.Query<CatalogTrack>(sql, parameters);
            }
        }

        private string GetFieldWordsSum(string fieldName, string[] words)
        {
            if (words.Length == 0)
                return null;
            
            return string.Join("+", words.Select(w => $"(case when {fieldName} like '%{w}%' then 1 else 0 end)").ToArray());
        }

        public void AddTracks(IEnumerable<CatalogTrack> tracks, Catalog catalog)
        {
            const string sql = "insert into Tracks (TrackName, Performer, Composer, Synchronisation, Mechanical, Performance, [Catalog]) values (@TrackName, @Performer, @Composer, @Synchronisation, @Mechanical, @Performance, @Catalog)";

            using (IDbConnection conn = new SQLiteConnection(connStr))
            {
                foreach (var track in tracks)
                {
                    var parameters = new DynamicParameters(new
                    {
                        track.TrackName,
                        track.Performer,
                        track.Composer,
                        track.Synchronisation,
                        track.Mechanical,
                        track.Performance,
                        Catalog = catalog.Name
                    });
                    conn.Execute(sql, parameters);
                }
            }
        }

        public string[] GetCatalogsList()
        {
            using (IDbConnection conn = new SQLiteConnection(connStr))
            {
                var res = conn.Query<string>("select Name from Catalogs");
                return res.ToArray();
            }
        }

        public void RemoveCatalog(string catalogName)
        {
            using (IDbConnection conn = new SQLiteConnection(connStr))
            {
                var parameters = new DynamicParameters(new { Catalog = catalogName });
                conn.Execute("delete from Tracks where [Catalog] = @Catalog", parameters);
                conn.Execute("delete from Catalogs where Name = @Catalog", parameters);
            }
        }
    }
}
