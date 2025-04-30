using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;

namespace MaciScriptIDE
{
    public partial class MainWindow : Window
    {
        private string currentProjectPath = "";
        private readonly Dictionary<string, TextEditor> openFiles = [];
        private readonly Dictionary<string, bool> modifiedFiles = [];

        public MainWindow()
        {
            InitializeComponent();
            KeyDown += Window_KeyDown;
            LoadSyntaxHighlighting();

            // Load application settings
            ApplicationSettingsManager.Instance.Load();
            ApplyTheme(ApplicationSettingsManager.Instance.Settings.IsDarkMode);

            // Set the dark mode checkbox based on loaded settings
            DarkModeCheckBox.IsChecked = ApplicationSettingsManager.Instance.Settings.IsDarkMode;
        }

        private void DarkModeCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool isDarkMode = DarkModeCheckBox.IsChecked ?? false;
            ApplyTheme(isDarkMode);

            // Save the setting
            ApplicationSettingsManager.Instance.Settings.IsDarkMode = isDarkMode;
            ApplicationSettingsManager.Instance.Save();
        }

        // Apply theme to all visual elements
        private void ApplyTheme(bool isDarkMode)
        {
            if (isDarkMode)
            {
                // Apply dark theme
                Resources["WindowBackgroundBrush"] = Resources["DarkBackgroundBrush"];
                Resources["WindowForegroundBrush"] = Resources["DarkForegroundBrush"];
                Resources["TabBackgroundBrush"] = Resources["DarkTabBackgroundBrush"];
                Resources["TabBorderBrush"] = Resources["DarkTabBorderBrush"];
                Resources["TreeViewBackgroundBrush"] = Resources["DarkTreeViewBackgroundBrush"];

                // Set the TreeView foreground explicitly to ensure text is white
                fileExplorer.Foreground = Brushes.White;

                // Also update the tree view items style to ensure all items have white text
                Style treeViewItemStyle = new(typeof(TreeViewItem));
                treeViewItemStyle.Setters.Add(new Setter(ForegroundProperty, Brushes.White));
                fileExplorer.Resources[typeof(TreeViewItem)] = treeViewItemStyle;

                // Make sure the settings panel text is also white
                DarkModeCheckBox.Foreground = Brushes.White;
            }
            else
            {
                // Apply light theme
                Resources["WindowBackgroundBrush"] = Resources["LightBackgroundBrush"];
                Resources["WindowForegroundBrush"] = Resources["LightForegroundBrush"];
                Resources["TabBackgroundBrush"] = Resources["LightTabBackgroundBrush"];
                Resources["TabBorderBrush"] = Resources["LightTabBorderBrush"];
                Resources["TreeViewBackgroundBrush"] = Resources["LightTreeViewBackgroundBrush"];

                // Reset the TreeView foreground to black
                fileExplorer.Foreground = Brushes.Black;

                // Update tree view items style for light mode
                Style treeViewItemStyle = new(typeof(TreeViewItem));
                treeViewItemStyle.Setters.Add(new Setter(ForegroundProperty, Brushes.Black));
                fileExplorer.Resources[typeof(TreeViewItem)] = treeViewItemStyle;

                // Reset settings panel text color
                DarkModeCheckBox.Foreground = Brushes.Black;
            }

            // Apply theme to any open editors
            foreach (var editor in openFiles.Values)
            {
                UpdateEditorTheme(editor, isDarkMode);
            }
        }

        // Update theme settings for a specific editor
        private void UpdateEditorTheme(TextEditor editor, bool isDarkMode)
        {
            if (isDarkMode)
            {
                editor.Background = (SolidColorBrush)Resources["DarkBackgroundBrush"];
                editor.Foreground = (SolidColorBrush)Resources["DarkForegroundBrush"];
            }
            else
            {
                editor.Background = (SolidColorBrush)Resources["LightBackgroundBrush"];
                editor.Foreground = (SolidColorBrush)Resources["LightForegroundBrush"];
            }

            // You may also need to update syntax highlighting colors based on the theme
            // This can be more complex and might require different XSHD files for light/dark
        }

        // File Explorer Methods
        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                currentProjectPath = dialog.SelectedPath;
                Title = $"MaciScript IDE - {Path.GetFileName(currentProjectPath)}";
                LoadProjectRoot(currentProjectPath);
            }
        }

        private void LoadProjectRoot(string path)
        {
            fileExplorer.Items.Clear();
            DirectoryInfo rootDir = new(path);
            TreeViewItem rootItem = new()
            {
                Header = rootDir.Name,
                Tag = rootDir.FullName,
                IsExpanded = true
            };

            // Add a dummy item to enable expanding
            rootItem.Items.Add(CreateDummyNode());

            // Handle the expansion of this root node immediately
            rootItem.Expanded += DirectoryItem_Expanded;

            fileExplorer.Items.Add(rootItem);

            // Since the root is already expanded, populate it immediately
            PopulateOnExpand(rootItem);
        }

        private static TreeViewItem CreateDummyNode()
        {
            return new TreeViewItem { Header = "Loading..." };
        }

        private void DirectoryItem_Expanded(object sender, RoutedEventArgs e)
        {
            if (sender is TreeViewItem treeViewItem)
            {
                // Only populate if this is the first expansion (contains dummy node)
                if (treeViewItem.Items.Count == 1 &&
                    treeViewItem.Items[0] is TreeViewItem firstItem &&
                    firstItem.Header.ToString() == "Loading...")
                {
                    PopulateOnExpand(treeViewItem);
                }

                // Remove the event to prevent it from firing again
                treeViewItem.Expanded -= DirectoryItem_Expanded;
            }
        }

        private void PopulateOnExpand(TreeViewItem treeViewItem)
        {
            if (treeViewItem.Tag is not string path)
                return;

            try
            {
                // Clear the dummy item
                treeViewItem.Items.Clear();

                DirectoryInfo directoryInfo = new(path);

                // Add subdirectories
                foreach (var dir in directoryInfo.GetDirectories())
                {
                    TreeViewItem dirNode = new()
                    {
                        Header = dir.Name,
                        Tag = dir.FullName
                    };

                    // Add a dummy item to enable expanding
                    dirNode.Items.Add(CreateDummyNode());

                    // Set up the expanded event
                    dirNode.Expanded += DirectoryItem_Expanded;

                    treeViewItem.Items.Add(dirNode);
                }

                // Add files
                foreach (var file in directoryInfo.GetFiles())
                {
                    TreeViewItem fileNode = new()
                    {
                        Header = file.Name,
                        Tag = file.FullName
                    };
                    treeViewItem.Items.Add(fileNode);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading directory: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FileExplorer_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem selectedItem)
            {
                string? path = selectedItem.Tag.ToString();
                if (File.Exists(path))
                {
                    OpenFileInTab(path);
                }
            }
        }

        private void OpenFileInTab(string filePath)
        {
            // Check if file is already open
            if (openFiles.ContainsKey(filePath))
            {
                // Find and select the existing tab
                foreach (TabItem tab in editorTabs.Items)
                {
                    if (tab.Tag?.ToString() == filePath)
                    {
                        editorTabs.SelectedItem = tab;
                        break;
                    }
                }
                return;
            }

            try
            {
                // Create the editor control in code
                var textEditor = new TextEditor
                {
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 14,
                    ShowLineNumbers = true,
                    WordWrap = false
                };

                ApplySyntaxHighlighting(textEditor, filePath);
                UpdateEditorTheme(textEditor, ApplicationSettingsManager.Instance.Settings.IsDarkMode);

                // Load content
                string content = File.ReadAllText(filePath);
                textEditor.Text = content;

                // Set up text changed event
                textEditor.TextChanged += (s, args) =>
                {
                    // Mark as modified
                    string tabPath = filePath; // Use local variable instead of accessing tab property

                    if (!modifiedFiles.ContainsKey(tabPath) || !modifiedFiles[tabPath])
                    {
                        modifiedFiles[tabPath] = true;
                        UpdateTabHeader(tabPath, true);
                    }
                };

                // Create tab header with filename
                string fileName = Path.GetFileName(filePath);
                var closeButton = new Button
                {
                    Content = "×",
                    Margin = new Thickness(5, 0, 0, 0),
                    Padding = new Thickness(3, 0, 3, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                closeButton.Click += CloseTabButton_Click;

                var header = new DockPanel();
                var headerText = new TextBlock { Text = fileName, VerticalAlignment = VerticalAlignment.Center };
                DockPanel.SetDock(closeButton, Dock.Right);
                header.Children.Add(closeButton);
                header.Children.Add(headerText);

                // Create tab with our custom style (should be applied automatically via TabControl.Resources)
                var newTab = new TabItem
                {
                    Header = header,
                    Content = textEditor,
                    Tag = filePath
                };

                // Add to collections
                openFiles[filePath] = textEditor;
                modifiedFiles[filePath] = false;

                // Add to UI
                editorTabs.Items.Add(newTab);
                editorTabs.SelectedItem = newTab;

                // Update window title
                Title = $"MaciScript IDE - {fileName}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateTabHeader(string filePath, bool isModified)
        {
            // Find the tab
            foreach (TabItem tab in editorTabs.Items)
            {
                if (tab.Tag.ToString() == filePath)
                {
                    // Get the header panel
                    DockPanel panel = (DockPanel)tab.Header;

                    // Get the text block (second child after close button)
                    TextBlock headerText = (TextBlock)panel.Children[1];

                    // Update header
                    string fileName = Path.GetFileName(filePath);
                    headerText.Text = isModified ? fileName + "*" : fileName;
                    break;
                }
            }
        }

        private void CloseTabButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the button that was clicked
            if (sender is not Button button) return;

            // Get the parent DockPanel
            if (button.Parent is not DockPanel panel) return;

            // Find the TabItem that contains this button
            TabItem? tabToClose = null;
            foreach (TabItem tab in editorTabs.Items)
            {
                if (tab.Header == panel)
                {
                    tabToClose = tab;
                    break;
                }
            }

            if (tabToClose == null) return;

            // Get the file path from the tab
            string? filePath = tabToClose.Tag?.ToString();
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            // Check for unsaved changes
            bool isModified = modifiedFiles.ContainsKey(filePath) && modifiedFiles[filePath];

            if (isModified)
            {
                string fileName = Path.GetFileName(filePath);
                var result = MessageBox.Show($"Save changes to {fileName}?", "Unsaved Changes",
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                if (result == MessageBoxResult.Cancel)
                    return;

                if (result == MessageBoxResult.Yes)
                {
                    // Save file
                    if (openFiles.TryGetValue(filePath, out var editor))
                    {
                        File.WriteAllText(filePath, editor.Text);
                    }
                }
            }

            // Store the index of the tab to select after closing
            int index = editorTabs.Items.IndexOf(tabToClose);

            // THIS IS THE KEY FIX: Setting Template to null before removing
            tabToClose.Template = null;

            // Remove from dictionaries
            openFiles.Remove(filePath);
            modifiedFiles.Remove(filePath);

            // Remove tab
            editorTabs.Items.Remove(tabToClose);

            // Select appropriate tab after closing
            if (editorTabs.Items.Count > 0)
            {
                // Select the next tab or the previous one if we closed the last tab
                if (index >= editorTabs.Items.Count)
                    index = editorTabs.Items.Count - 1;

                editorTabs.SelectedIndex = index;
            }

            // Update title if no tabs remain
            if (editorTabs.Items.Count == 0)
                Title = "MaciScript IDE";
        }

        // Event handlers - make sure these are properly defined in your class
        private void TextEditor_TextChanged(object sender, EventArgs e)
        {
            // Mark as modified
            if (sender is not TextEditor textEditor) return;

            // Find the tab containing this editor
            foreach (TabItem tab in editorTabs.Items)
            {
                if (tab.Content == textEditor)
                {
                    string? tabPath = tab.Tag?.ToString();
                    if (string.IsNullOrEmpty(tabPath)) continue;

                    if (!modifiedFiles.ContainsKey(tabPath) || !modifiedFiles[tabPath])
                    {
                        modifiedFiles[tabPath] = true;
                        UpdateTabHeader(tabPath, true);
                    }
                    break;
                }
            }
        }

        private void Document_Changed(object sender, DocumentChangeEventArgs e)
        {
            // This is just a placeholder - adjust as needed for your specific setup
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+S to save
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SaveAllFiles();
                e.Handled = true;
            }
        }

        // Add this method to save all modified files
        private void SaveAllFiles()
        {
            // If no modified files, no need to do anything
            if (!modifiedFiles.Any(kv => kv.Value))
                return;

            // Save each modified file
            foreach (var filePath in modifiedFiles.Keys.ToList())
            {
                if (modifiedFiles[filePath] && openFiles.ContainsKey(filePath))
                {
                    try
                    {
                        File.WriteAllText(filePath, openFiles[filePath].Text);

                        // Update modified status
                        modifiedFiles[filePath] = false;
                        UpdateTabHeader(filePath, false);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private static void ApplySyntaxHighlighting(TextEditor editor, string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();

            // Set the appropriate highlighting definition based on file extension
            editor.SyntaxHighlighting = extension switch
            {
                ".cs" => HighlightingManager.Instance.GetDefinition("C#"),
                ".xml" or ".xaml" or ".config" or ".csproj" => HighlightingManager.Instance.GetDefinition("XML"),
                ".js" => HighlightingManager.Instance.GetDefinition("JavaScript"),
                ".html" or ".htm" => HighlightingManager.Instance.GetDefinition("HTML"),
                ".css" => HighlightingManager.Instance.GetDefinition("CSS"),
                ".json" => HighlightingManager.Instance.GetDefinition("JavaScript"),
                ".sql" => HighlightingManager.Instance.GetDefinition("SQL"),
                ".ps1" => HighlightingManager.Instance.GetDefinition("PowerShell"),
                ".py" => HighlightingManager.Instance.GetDefinition("Python"),
                ".vb" => HighlightingManager.Instance.GetDefinition("VB"),
                ".cpp" or ".h" or ".hpp" => HighlightingManager.Instance.GetDefinition("C++"),
                ".maci" => HighlightingManager.Instance.GetDefinition("MaciScript"),
                _ => null,
            };
        }

        private static void LoadSyntaxHighlighting()
        {
            try
            {
                using var reader = new XmlTextReader("MaciScript.xshd");
                var highlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);

                HighlightingManager.Instance.RegisterHighlighting("MaciScript", [".maci"], highlighting);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error loading syntax highlighting: {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // Class for editor tabs
    public class EditorTab : INotifyPropertyChanged
    {
        private string _header = "";
        private bool _isModified;

        public string FilePath { get; set; } = "";
        public TextDocument EditorDocument { get; set; }

        public string Header
        {
            get => _isModified ? _header + "*" : _header;
            set
            {
                _header = value;
                OnPropertyChanged(nameof(Header));
            }
        }

        public bool IsModified
        {
            get => _isModified;
            set
            {
                if (_isModified != value)
                {
                    _isModified = value;
                    OnPropertyChanged(nameof(IsModified));
                    OnPropertyChanged(nameof(Header));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Constructor
        public EditorTab()
        {
            EditorDocument = new TextDocument();

            // Set up document change tracking
            EditorDocument.Changed += (s, e) =>
            {
                if (!IsModified)
                    IsModified = true;
            };
        }
    }
}