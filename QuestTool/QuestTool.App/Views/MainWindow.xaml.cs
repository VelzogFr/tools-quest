using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QuestTool.App.Models;
using QuestTool.App.Services;

namespace QuestTool.App.Views
{
    public partial class MainWindow : Window
    {
        private readonly DatabaseService _db;
        private readonly SchemaService _schemaService;
        private readonly string[] _tableNames;

        public MainWindow()
        {
            InitializeComponent();

            var cs = ConfigurationManager.ConnectionStrings["DefaultConnection"];
            if (cs == null)
            {
                MessageBox.Show("ConnectionStrings[DefaultConnection] manquante dans App.config", "Configuration", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }

            _db = new DatabaseService(cs.ConnectionString, cs.ProviderName);
            _schemaService = new SchemaService(_db);
            var list = ConfigurationManager.AppSettings["TableNames"] ?? string.Empty;
            _tableNames = list.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();

            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            foreach (var table in _tableNames)
            {
                try
                {
                    var schema = await _schemaService.LoadTableSchemaAsync(table);
                    var tab = BuildTableTab(schema);
                    TablesTab.Items.Add(tab);
                }
                catch (Exception ex)
                {
                    TablesTab.Items.Add(new TabItem { Header = table, Content = new TextBlock { Text = "Erreur: " + ex.Message } });
                }
            }
        }

        private TabItem BuildTableTab(TableSchema schema)
        {
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var panel = new StackPanel { Margin = new Thickness(16), Orientation = Orientation.Vertical };

            var grid = new Grid { Margin = new Thickness(0, 8, 0, 8) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            int row = 0;
            foreach (var column in schema.Columns)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var label = new TextBlock { Text = column.Name, Margin = new Thickness(0, 6, 12, 6), Opacity = 0.8 };
                Grid.SetRow(label, row);
                Grid.SetColumn(label, 0);

                var tb = new TextBox { Name = MakeControlName(schema.TableName, column.Name), Margin = new Thickness(0, 4, 0, 4), IsEnabled = !column.IsIdentity };
                tb.ToolTip = column.DataType + (column.IsNullable ? " (NULL)" : "") + (column.IsIdentity ? " [IDENTITY]" : string.Empty);

                Grid.SetRow(tb, row);
                Grid.SetColumn(tb, 1);

                grid.Children.Add(label);
                grid.Children.Add(tb);
                row++;
            }

            panel.Children.Add(grid);

            var save = new Button { Content = "Enregistrer", Margin = new Thickness(0, 12, 0, 0), Height = 36, MinWidth = 120 };
            save.Click += (s, e) => SaveTable(schema, grid);
            panel.Children.Add(save);

            scroll.Content = panel;

            return new TabItem { Header = schema.TableName, Content = scroll };
        }

        private string MakeControlName(string table, string column)
        {
            return ("_" + table + "_" + column).Replace('-', '_');
        }

        private async void SaveTable(TableSchema schema, Grid grid)
        {
            try
            {
                var values = new Dictionary<string, object>();
                foreach (var col in schema.Columns)
                {
                    if (col.IsIdentity) continue;
                    var tb = grid.Children.OfType<TextBox>().FirstOrDefault(x => x.Name == MakeControlName(schema.TableName, col.Name));
                    if (tb == null) continue;
                    var text = (tb.Text ?? string.Empty).Trim();
                    if (text.Length == 0)
                    {
                        // Essaye de propager le quest_id global si applicable
                        if (string.Equals(col.Name, "quest_id", StringComparison.OrdinalIgnoreCase) || string.Equals(col.Name, "id_quest", StringComparison.OrdinalIgnoreCase))
                        {
                            text = (QuestIdTextBox.Text ?? string.Empty).Trim();
                        }
                    }
                    if (text.Length == 0) continue;

                    object value = text;
                    values[col.Name] = value;
                }

                if (!values.Keys.Any(k => string.Equals(k, "quest_id", StringComparison.OrdinalIgnoreCase) || string.Equals(k, "id_quest", StringComparison.OrdinalIgnoreCase)))
                {
                    var globalQuestId = (QuestIdTextBox.Text ?? string.Empty).Trim();
                    if (globalQuestId.Length > 0)
                    {
                        var matching = schema.Columns.FirstOrDefault(c => string.Equals(c.Name, "quest_id", StringComparison.OrdinalIgnoreCase) || string.Equals(c.Name, "id_quest", StringComparison.OrdinalIgnoreCase));
                        if (matching != null && !matching.IsIdentity)
                        {
                            values[matching.Name] = globalQuestId;
                        }
                    }
                }

                if (values.Count == 0)
                {
                    MessageBox.Show("Aucune donnée à enregistrer.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                await _db.InsertAsync(schema.TableName, values);
                MessageBox.Show("Enregistré avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de l'enregistrement: " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}