using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace DataSeries
{
    public class DataSeries<T>
    {
        private readonly IEnumerable<T> _data;

        private DataSeries(IEnumerable<T> data) => _data = data;

        public static DataSeries<T> From(IEnumerable<T> source)
            => new DataSeries<T>(source);

        public static DataSeries<T> FromCsv(string path, Func<string[], T> parser)
        {
            List<T> data = new List<T>();

            try
            {
                List<string> content  = File.ReadAllLines(path).ToList();
                foreach (string line in content.Skip(1))
                {
                    string[] cols = line.Split(',');
                    data.Add(parser(cols));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Erreur d'ouverture fichier {0}", e);
                throw;
            }
            return From(data);
        }

        public int Count => _data.Count();
        public IEnumerable<T> Values => _data;
    }
}