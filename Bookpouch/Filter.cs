using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Bookpouch
{
    partial class MainWindow
    {
        public BookFilter Filter = new BookFilter();

        /// <summary>
        /// Assemble query to pull out book list out of the database, implementing the provided filter
        /// </summary>
        /// <param name="filter">Dictionary with the filter parameters</param>
        /// <returns>Assembled query, ready for execution</returns>
        private Tuple<string, SQLiteParameter[]> AssembleQuery()
        {
            var sqlConditions = new List<string>();
            var sqlWhere = String.Empty;
            var parameters = new List<SQLiteParameter>();


            if (!String.IsNullOrEmpty(Filter.Title)) //Title filter
            {
                sqlConditions.Add("Title LIKE @Title");
                parameters.Add(new SQLiteParameter("Title", "%" + Filter.Title + "%"));
            }

            if (!String.IsNullOrEmpty(Filter.Author)) //Author filter
            {
                sqlConditions.Add("Author = @Author");
                parameters.Add(new SQLiteParameter("Author", Filter.Author));
            }

            if (!String.IsNullOrEmpty(Filter.Publisher)) //Publisher filter
            {
                sqlConditions.Add("Publisher = @Publisher");
                parameters.Add(new SQLiteParameter("Publisher", Filter.Publisher));
            }

            if (!String.IsNullOrEmpty(Filter.Language)) //Language filter
            {
                sqlConditions.Add("Language = @Language");
                parameters.Add(new SQLiteParameter("Language", Filter.Language));
            }

            if (Filter.Created != default(DateTime)) //Published filter
            {
                sqlConditions.Add("Published = @Published");
                parameters.Add(new SQLiteParameter("Published", Filter.Published));
            }

            if (!String.IsNullOrEmpty(Filter.Description)) //Description filter
            {
                sqlConditions.Add("Description LIKE @Description");
                parameters.Add(new SQLiteParameter("Description", "%" + Filter.Description + "%"));
            }

            if (!String.IsNullOrEmpty(Filter.Series)) //Series filter
            {
                sqlConditions.Add("Series = @Series");
                parameters.Add(new SQLiteParameter("Series", Filter.Series));
            }

            if (Filter.Created != default(DateTime)) //Created filter
            {
                sqlConditions.Add("Created = @Created");
                parameters.Add(new SQLiteParameter("Created", Filter.Created));
            }

            if (Filter.Favorite) //Favorite filter
            {
                sqlConditions.Add("Favorite = @Favorite");
                parameters.Add(new SQLiteParameter("Favorite", Filter.Favorite));
            }

            if (Filter.Sync) //Sync filter
            {
                sqlConditions.Add("Sync = @Sync");
                parameters.Add(new SQLiteParameter("Sync", Filter.Sync));
            }

            if (!String.IsNullOrEmpty(Filter.Category)) //Category filter
            {
                sqlConditions.Add("Category = @Category");
                parameters.Add(new SQLiteParameter("Category", Filter.Category));
            }

            if (!String.IsNullOrEmpty(Filter.Path)) //Path filter
            {
                sqlConditions.Add("Path LIKE @Path");
                parameters.Add(new SQLiteParameter("Path", "%" + Filter.Path + "%"));
            }

            Filter.ParameterCount = sqlConditions.Count;

            if (sqlConditions.Count > 0)
            {
                sqlWhere = "WHERE " + String.Join(" AND ", sqlConditions);
            }
           
            var sql = "SELECT * FROM books b LEFT JOIN categories c ON b.Path = c.Path " + sqlWhere + "  GROUP BY b.Path";

            return new Tuple<string, SQLiteParameter[]>(sql, parameters.ToArray());
        }

        /// <summary>
        /// Display filter parameters above the book grid
        /// </summary>
        private void GenerateFilterView()
        {
            if (Filter.ParameterCount == 0)
            {
                FilterWrap.Visibility = Visibility.Collapsed;
                return;
            }

            var keyNames = new Dictionary<string, string>
            {
                {"Title", UiLang.Get("BookGridHeaderTitle")},
                {"Author", UiLang.Get("BookGridHeaderAuthor")},
                {"Category", UiLang.Get("BookGridHeaderCategory")},
                {"Series", UiLang.Get("BookGridSeries")},
                
            };

            FilterList.Children.Clear();

            foreach (var field in typeof(BookFilter).GetFields())
            {
                if (!keyNames.ContainsKey(field.Name) || field.GetValue(Filter) == null)
                    continue;

                var value = new TextBlock
                {
                    Foreground = Brushes.DodgerBlue,
                    Text = (field.GetValue(Filter) ?? String.Empty).ToString()
                };

                var name = new TextBlock
                {
                    Foreground = Brushes.Gray,
                    Text = keyNames[field.Name] + ": "
                };

                var separator = new TextBlock
                {
                    Foreground = Brushes.Gray,
                    Text = "; "
                };

                FilterList.Children.Add(name);
                FilterList.Children.Add(value);
                FilterList.Children.Add(separator);


            }

            FilterWrap.Visibility = Visibility.Visible;
        }

        public class BookFilter : BookData
        {
            public int ParameterCount = 0;
            public bool SortByFavorite = false;
        }
    }
}
