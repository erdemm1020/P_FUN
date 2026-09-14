using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DataSeries
{
    public class DataSeries<T> : IEnumerable<T>
    {
        private readonly IReadOnlyList<T> _data;

        private DataSeries(IEnumerable<T> data) 
        {
            _data = data.ToList().AsReadOnly();
        }

        public static DataSeries<T> From(IEnumerable<T> source)
            => new DataSeries<T>(source);

        public static DataSeries<T> FromCsv(string path, Func<string[], T> parser)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Le fichier CSV est introuvable : {path}");
            }

            List<T> data = new List<T>();

            try
            {
                foreach (string line in File.ReadLines(path).Skip(1))
                {
                    if (string.IsNullOrWhiteSpace(line)) 
                        continue; 

                    string[] cols = line.Split(',');
                    data.Add(parser(cols));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Erreur lors de la lecture du fichier : {e.Message}");
                throw;
            }

            return From(data);
        }

        public int Count => _data.Count;
        public IReadOnlyList<T> Values => _data;

        public IEnumerator<T> GetEnumerator() => _data.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}