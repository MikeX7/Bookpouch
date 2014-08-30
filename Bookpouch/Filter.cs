﻿using System;
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
                sqlConditions.Add("b.Title LIKE @Title");
                parameters.Add(new SQLiteParameter("Title", "%" + Filter.Title + "%"));
            }

            if (!String.IsNullOrEmpty(Filter.Author)) //Author filter
            {
                sqlConditions.Add("b.Author = @Author");
                parameters.Add(new SQLiteParameter("Author", Filter.Author));
            }

            if (!String.IsNullOrEmpty(Filter.Publisher)) //Publisher filter
            {
                sqlConditions.Add("b.Publisher = @Publisher");
                parameters.Add(new SQLiteParameter("Publisher", Filter.Publisher));
            }

            if (!String.IsNullOrEmpty(Filter.Language)) //Language filter
            {
                sqlConditions.Add("b.Language = @Language");
                parameters.Add(new SQLiteParameter("Language", Filter.Language));
            }

            if (Filter.Published != null) //Published filter
            {
                sqlConditions.Add("b.Published = @Published");
                parameters.Add(new SQLiteParameter("Published", Filter.Published));                
            }

            if (!String.IsNullOrEmpty(Filter.Description)) //Description filter
            {
                sqlConditions.Add("b.Description LIKE @Description");
                parameters.Add(new SQLiteParameter("Description", "%" + Filter.Description + "%"));
            }

            if (!String.IsNullOrEmpty(Filter.Series)) //Series filter
            {
                sqlConditions.Add("b.Series = @Series");
                parameters.Add(new SQLiteParameter("Series", Filter.Series));
            }

            if (Filter.Created != default(DateTime)) //Created filter
            {
                sqlConditions.Add("b.Created = @Created");
                parameters.Add(new SQLiteParameter("Created", Filter.Created));
            }

            if (Filter.Favorite) //Favorite filter
            {
                sqlConditions.Add("b.Favorite = @Favorite");
                parameters.Add(new SQLiteParameter("Favorite", Filter.Favorite));
            }

            if (Filter.Sync) //Sync filter
            {
                sqlConditions.Add("b.Sync = @Sync");
                parameters.Add(new SQLiteParameter("Sync", Filter.Sync));
            }

            if (!String.IsNullOrEmpty(Filter.Category)) //Category filter
            {
                sqlConditions.Add("c.Name = @Category");
                parameters.Add(new SQLiteParameter("Category", Filter.Category));
            }

            if (!String.IsNullOrEmpty(Filter.Path)) //Path filter
            {
                sqlConditions.Add("b.Path LIKE @Path");
                parameters.Add(new SQLiteParameter("Path", "%" + Filter.Path + "%"));
            }

            Filter.ParameterCount = sqlConditions.Count;

            if (sqlConditions.Count > 0)
            {
                sqlWhere = "WHERE " + String.Join(" AND ", sqlConditions);
            }

            var sql = "SELECT *, GROUP_CONCAT(c.Name, ', ') Categories FROM books b LEFT JOIN categories c ON b.Path = c.Path " + sqlWhere + "  GROUP BY b.Path";

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
            
            var keyNames = new Dictionary<string, string> //Localized names of the displayed search parameters
            {
                {"Title", UiLang.Get("FieldTitle")},
                {"Author", UiLang.Get("FieldAuthor")},
                {"Publisher", UiLang.Get("FieldPublisher")},
                {"Language", UiLang.Get("FieldLanguage")},
                {"Published", UiLang.Get("FieldPublished")},
                {"Description", UiLang.Get("FieldDescription")},
                {"Series", UiLang.Get("FieldSeries")},
                {"Created", UiLang.Get("FieldCreated")},
                {"Favorite", UiLang.Get("FieldFavorite")},
                {"Sync", UiLang.Get("FieldSync")},
                {"Category", UiLang.Get("FieldCategory")},
                {"Path", UiLang.Get("FieldPath")},                
                
            };
            
            FilterList.Children.Clear();

            foreach (var field in typeof(BookFilter).GetFields())
            {
                if (!keyNames.ContainsKey(field.Name) || field.GetValue(Filter) == null || field.GetValue(Filter).ToString() == String.Empty)
                    continue;

                var txtValue = field.GetValue(Filter);
                
                //Handle some special types like booleans for better displaying
                if (field.FieldType == typeof (DateTime) || field.FieldType == typeof (DateTime?))
                {
                    if((DateTime) txtValue == DateTime.MinValue)
                        continue;
                   
                    txtValue = ((DateTime) txtValue).Date.ToString("d");
                }
                else if (field.FieldType == typeof (Boolean) && (Boolean) txtValue == false)
                    continue;

                var value = new TextBlock
                {
                    Foreground = Brushes.DodgerBlue,
                    Text = (txtValue ?? String.Empty).ToString()
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
            public string Category;
        }
    }
}