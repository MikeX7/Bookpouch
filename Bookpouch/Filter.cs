using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bookpouch
{
    partial class MainWindow
    {
        /// <summary>
        /// Assemble query to pull out book list out of the database, implementing the provided filter
        /// </summary>
        /// <param name="filter">Dictionary with the filter parameters</param>
        /// <returns>Assembled query, ready for execution</returns>
        private static Tuple<string, SQLiteParameter[]> AssembleQuery(IReadOnlyDictionary<string, string> filter)
        {
            var sqlConditions = new List<string>();
            var sqlWhere = String.Empty;
            var parameters = new List<SQLiteParameter>();


            if (filter.ContainsKey("title"))
            {
                sqlConditions.Add("b.Title LIKE @Title");
                parameters.Add(new SQLiteParameter("Title", "%" + filter["title"] + "%"));
            }

            if (filter.ContainsKey("category"))
            {
                sqlConditions.Add("c.Name = @Category");
                parameters.Add(new SQLiteParameter("Category", filter["category"]));
            }

            if (filter.ContainsKey("series"))
            {
                sqlConditions.Add("b.Series = @Series");
                parameters.Add(new SQLiteParameter("Series", filter["series"]));
            }

            if (filter.ContainsKey("favorite"))
            {
                sqlConditions.Add("b.Favorite = @Favorite");
                parameters.Add(new SQLiteParameter("Favorite", filter["favorite"]));
            }

            if (filter.ContainsKey("sync"))
            {
                sqlConditions.Add("b.Sync = @Sync");
                parameters.Add(new SQLiteParameter("Sync", filter["sync"]));
            }

            if (sqlConditions.Count > 0)
            {
                sqlWhere = "WHERE " + String.Join(" AND ", sqlConditions);
            }
           
            var sql = "SELECT * FROM books b LEFT JOIN categories c ON b.Path = c.Path " + sqlWhere + "  GROUP BY b.Path";

            return new Tuple<string, SQLiteParameter[]>(sql, parameters.ToArray());
        }
    }
}
