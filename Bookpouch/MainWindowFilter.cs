using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Bookpouch
{
    partial class MainWindow
    {
        public BookFilter Filter = new BookFilter();

        /// <summary>
        /// Assemble query to pull out book list out of the database, implementing the provided filter
        /// </summary>
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
                sqlConditions.Add("b.Published " + (Filter.PublishedRange == 0 ? "=" : (Filter.PublishedRange == 1 ? ">" : "<")) + " @Published");
                parameters.Add(new SQLiteParameter("Published", Filter.Published.Value.ToString("yyyy-MM-dd")));                
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
                sqlConditions.Add("b.Created " + (Filter.CreatedRange == 0 ? "=" : (Filter.CreatedRange == 1 ? ">" : "<")) + " @Created");
                parameters.Add(new SQLiteParameter("Created", Filter.Created.ToString("yyyy-MM-dd")));
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

            var sql = "SELECT *, GROUP_CONCAT(c.Name, ', ') Categories FROM books b LEFT JOIN categories c ON b.Path = c.Path " + sqlWhere + " COLLATE NOCASE  GROUP BY b.Path";

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

        private readonly ObservableCollection<FilterPreset> _presetList = new ObservableCollection<FilterPreset>();

        public void GenerateFilterPresetList()
        {   
            _presetList.Clear();

            using (var query = Db.Query("SELECT * FROM filters"))
            {
                while (query.Read())
                {
                    var bs = new BinaryFormatter();
                    var parameters = (BookFilter) bs.Deserialize(new MemoryStream((byte[]) query["Parameters"]));

                    _presetList.Add(new FilterPreset
                    {
                        Name = query["Name"].ToString(),
                        BookFilter = parameters
                    });
                }
            }

            FilterPresetList.ItemsSource = _presetList;
        }

        private void SavePreset_OnClick(object sender, RoutedEventArgs e)
        {
            FilterPresetName.Visibility = Visibility.Visible;
            FilterPresetName.Focus();
        } 

        private void FilterPresetName_OnKeyUp(object sender, KeyEventArgs e)
        {
            var textBox = (TextBox) sender;

            if(e.Key != Key.Enter && e.Key != Key.Escape)
                return;

            if (e.Key == Key.Enter)
            {
                using (var ms = new MemoryStream())
                {
                    var bf = new BinaryFormatter();
                    bf.Serialize(ms, Filter);                    


                    using (var query = Db.Query("INSERT OR IGNORE INTO filters VALUES(@Name, @Parameters)", new[]
                    {
                        new SQLiteParameter("Name", textBox.Text),
                        new SQLiteParameter("Parameters", ms.GetBuffer())
                    }))
                    {

                        if (query.RecordsAffected > 0)
                        {
                            Info(UiLang.Get("FilterSavingPresetSuccessful"));
                            GenerateFilterPresetList();
                        }
                        else
                        {
                            Info(UiLang.Get("FilterSavingPresetDuplicate"), 1);
                            return;
                        }
                    }
                }
                

                textBox.Text = String.Empty;


            }

            FilterPresetName.Visibility = Visibility.Collapsed; 
        }        

        /// <summary>
        /// Set the active filter to the saved preset
        /// </summary>        
        private void SetFilterPreset_OnClick(object sender, RoutedEventArgs e)
        {
            Filter = ((FilterPreset) ((Button) sender).DataContext).BookFilter;
            BookGridReload();
        }
        
        private void SetFilterPreset_OnPreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var button = (Button) sender;
            var filterPreset = (FilterPreset) button.DataContext;

            if (
                    MessageBox.Show(
                        String.Format(UiLang.Get("DiscardFilterPresetConfirm"),
                            button.Content), UiLang.Get("DiscardFilterPreset"), MessageBoxButton.YesNo) !=
                    MessageBoxResult.Yes)
                return;

            Db.NonQuery("DELETE FROM filters WHERE Name = @Name", new []{new SQLiteParameter("Name", filterPreset.Name) });

            _presetList.Remove(filterPreset);
        }

        [Serializable]
        public class BookFilter : BookData
        {
            public int ParameterCount = 0;
            public bool SortByFavorite = false;
            public string Category;
            public int PublishedRange = 0;
            public int CreatedRange = 0;
        }

        private class FilterPreset
        {
            public string Name { set; get; }
            public BookFilter BookFilter { set; get; }
        }

    }
}
