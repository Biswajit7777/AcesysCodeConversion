using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Fls.AcesysConversion.Common;
using Fls.AcesysConversion.Common.Enums;
using Fls.AcesysConversion.PLC.Rockwell.Components;
using Fls.AcesysConversion.PLC.Siemens.Components;
using Fls.AcesysConversion.UI.Messages;
using Fls.AcesysConversion.UI.Services.Interface;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;

namespace Fls.AcesysConversion.UI.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string? coreRefXml;

        [ObservableProperty]
        private string? clxConfigXml;

        [ObservableProperty]
        private string? pntConfigXml;

        [ObservableProperty]
        private string? fileName;

        [ObservableProperty]
        private bool isDirty;

        [ObservableProperty]
        private bool isFileLoaded;

        [ObservableProperty]
        private bool isNotUpgrading;

        [ObservableProperty]
        private string loadingIndicatorText = "Ready...";

        [ObservableProperty]
        private Brush loadingIndicatorBackground = Brushes.Green;

        [ObservableProperty]
        private BlockSelection blockSelect = BlockSelection.Default;

        [ObservableProperty]
        private InterlockSelection interlockSelect = InterlockSelection.Default;

        [ObservableProperty]
        private FacePlateAttributeMapping mapBy = FacePlateAttributeMapping.Name;

        [ObservableProperty]
        private string? sourceWithoutIdXmlText;

        private static string? sourceXmlText;

        private static string? targetXmlText;

        [ObservableProperty]
        private string? targetWithoutIdXmlText;        

        [ObservableProperty]
        private int targetCurrentLine;

        [ObservableProperty]
        private int sourceCurrentLine;

        [ObservableProperty]
        private string timeElapsed;

        [ObservableProperty]
        private string? sourceWithoutIdAWLText;

        private static string? sourceAWLText;

        private static string? sourceSDFText;

        private static string? targetAWLText;

        [ObservableProperty]
        private string? targetWithoutIdAWLText;

        private ObservableCollection<string> _items;

        private ObservableCollection<string> _S7items;

        private readonly List<KeyValuePair<int, int>> targetIdToLineMapping = new();
        private readonly List<KeyValuePair<int, int>> sourceIdToLineMapping = new();

        private RockwellL5XProject xmlDocument = new();
        private RockwellL5XProject xmlDocumentReplaced = new();

        private SiemensProject awlDocumentReplaced = new();
        public IRelayCommand? OpenFileCommand { get; }

        public IRelayCommand? OpenS7FileCommand { get; }

        public IRelayCommand? OpenECSFileCommand { get; }
        public IRelayCommand? UpgradeFileCommand { get; }
        public IRelayCommand? UpgradeS7FileCommand { get; }
        public IRelayCommand? CloseFileCommand { get; }
        public IRelayCommand? SaveFileCommand { get; }
       

        public IRelayCommand? UserMessageDoubleClickCommand { get; }

        // Commands
        public IRelayCommand? SaveECSFileCommand { get; }

        public IRelayCommand LoadECSFileCommand { get; }        


        private readonly MessageBroker messagesBroker = Ioc.Default.GetRequiredService<MessageBroker>();

        [ObservableProperty]
        private string progressText = string.Empty;

        private readonly IProgress<string> progress;
        private readonly Stopwatch stopwatch = new();
        private readonly DispatcherTimer dispatcherTimer = new();        

        List<string> ECSFilePaths = new List<string>();
        List<string> S7FilePaths = new List<string>();

        public ObservableCollection<string> Items
        {
            get => _items;
            set
            {
                _items = value;
                OnPropertyChanged(nameof(Items));
            }
        }

        public ObservableCollection<string> S7Items
        {
            get => _S7items;
            set
            {
                _S7items = value;
                OnPropertyChanged(nameof(Items));
            }
        }

        private int _selectedTabIndex;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                _selectedTabIndex = value;
                OnPropertyChanged(); 
            }
        }

        private Brush _siemensEllipseColor = Brushes.DarkGray;
        public Brush SiemensEllipseColor
        {
            get => _siemensEllipseColor;
            set
            {
                _siemensEllipseColor = value;
                OnPropertyChanged(); // Notify UI of the change
            }
        }

        public SiemensProject SiemensProject { get; set; }

        public MainWindowViewModel()
        {
            IsDirty = false;
            IsFileLoaded = false;
            IsNotUpgrading = true;

            FileName = null;
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);

            OpenFileCommand = new RelayCommand(OnOpenFileClick, CanOpenFile);
            OpenS7FileCommand = new RelayCommand(OnOpenS7FileClick, CanS7OpenFile);
            OpenECSFileCommand = new RelayCommand(OnOpenECSFileClick, CanECSOpenFile);
            UpgradeFileCommand = new RelayCommand(OnUpgradeFileClick, CanUpgradeFile);
            UpgradeS7FileCommand = new RelayCommand(OnUpgradeS7FileClick, CanUpgradeS7File);
            CloseFileCommand = new RelayCommand(OnCloseFileClick, CanCloseFile);
            SaveFileCommand = new RelayCommand(OnSaveFileClick, CanSaveFile);
            SaveECSFileCommand = new RelayCommand(OnSaveECSFileClick, CanECSSaveFile);
            UserMessageDoubleClickCommand = new RelayCommand<object?>(OnUserMessageDoubleClick);
            Items = new ObservableCollection<string>();
            S7Items = new ObservableCollection<string>();

            progress = new Progress<string>(ProgressHandler);

            Messenger.Register<MainWindowViewModel, UserMessageSelectionChanged>(this, (r, m) => r.UserMessageSelectionChangedHandler(m.Reference, m.OriginalReference));
        }

        private bool CanUpgradeS7File()
        {
            return true;
        }

        private async void OnUpgradeS7FileClick()
        {
            try
            {
                BeforeUpgradeSetup();

                // Use project instead of awlDocumentReplaced
                SiemensProject = await Task.Run(() => UpgradeFileS7(progress));

                progress.Report("Upgrade Complete, Formatting File");

                if (SiemensProject != null)
                {
                    TargetWithoutIdAWLText += "\n\nUpgrading Completed. Loading Upgraded Content, Please Wait...";

                    // Assuming sourceAWLText is related to project content in some way
                    targetXmlText = SiemensProject.ExtractAwlFile(); // Ensure ExtractAwlFile is available and returns the correct content

                    if (!string.IsNullOrEmpty(targetXmlText))
                    {
                        TargetWithoutIdAWLText = await Task.Run(() => RemoveFlsIdAttributes(targetXmlText!, targetIdToLineMapping));
                    }
                }

                AfterUpgradeSetup();
            }
            catch (Exception ex)
            {
                IMsgBoxService? msgBoxService = Ioc.Default.GetService<IMsgBoxService>();
                ShowErrorMessage(msgBoxService, ex);
            }
            finally
            {
                IsNotUpgrading = true;
                RaiseCanExecuteChanged();
            }
        }

        private bool CanS7OpenFile()
        {
            return !IsFileLoaded;
        }

        private async void OnOpenS7FileClick()
        {
            try
            {
                IOpenFileDlgVM? dialog = Ioc.Default.GetService<IOpenFileDlgVM>();

                if (dialog != null)
                {
                    // Clear previous file paths (if any)
                    S7FilePaths.Clear();
                    S7Items.Clear(); // Clear previous file names

                    // Loop to select two files (AWL and SDF)
                    for (int i = 0; i < 2; i++)
                    {
                        string fileType = i == 0 ? "AWL" : "SDF"; // Define the file type to load
                        string fileFilter = i == 0 ? "AWL Files (*.awl)|*.awl|All Files (*.*)|*.*" : "SDF Files (*.sdf)|*.sdf|All Files (*.*)|*.*";

                        // Open file dialog for AWL/SDF
                        string? filePath = dialog.OpenFileDlg($"{fileType} Files", fileFilter);

                        if (!string.IsNullOrEmpty(filePath))
                        {
                            // Add the full path to S7FilePaths
                            S7FilePaths.Add(filePath);

                            // Extract the file name and add it to Items list
                            string fileName = Path.GetFileName(filePath);
                            S7Items.Add(fileName);
                        }
                        else
                        {
                            // Exit if the user cancels the dialog
                            IMsgBoxService? msgBoxService = Ioc.Default.GetService<IMsgBoxService>();
                            return;
                        }
                    }

                    // Proceed only if exactly two files (AWL and SDF) are selected
                    if (S7FilePaths.Count == 2)
                    {
                        SetBusyStatus();

                        // Process the AWL file
                        FileName = S7FilePaths[0];
                        await LoadFileAWL(progress);

                        // Switch to Siemens tab after AWL processing
                        SelectedTabIndex = 1; // Index of the Siemens tab

                        // Change the Siemens Ellipse color to green
                        SiemensEllipseColor = Brushes.Green;

                        // Process the SDF file
                        FileName = S7FilePaths[1];
                        await LoadFileSDF(progress);

                        SetReadyStatus();
                    }
                    else
                    {
                        // Handle the case where two files were not selected
                        IMsgBoxService? msgBoxService = Ioc.Default.GetService<IMsgBoxService>();
                        ShowErrorMessage(msgBoxService, new Exception("AWL and SDF files must be selected."));
                    }
                }
            }
            catch (Exception ex)
            {
                IMsgBoxService? msgBoxService = Ioc.Default.GetService<IMsgBoxService>();
                ShowErrorMessage(msgBoxService, ex);
            }
            finally
            {
                RaiseCanExecuteChanged();
            }
        }

        private async Task LoadFileSDF(IProgress<string> progress)
        {
            if (!string.IsNullOrEmpty(FileName))
            {
                
                progress.Report("Loading SDF File");

                try
                {
                    // Read the CSV file
                    string sdfContent = await Task.Run(() => File.ReadAllText(FileName));
                    progress.Report("Processing SDF File...");

                    // Process and format the CSV content
                    sourceSDFText = await Task.Run(() => ProcessSdfContent(sdfContent));
                   
                }
                catch (Exception ex)
                {
                    // Handle any exceptions during file processing
                    IMsgBoxService? msgBoxService = Ioc.Default.GetService<IMsgBoxService>();
                    ShowErrorMessage(msgBoxService, ex);
                }
            }
            progress.Report("AWL & SDF File Load Complete");
            IsFileLoaded = true;
        }

        private static string ProcessSdfContent(string sdfContent)
        {
            StringBuilder formattedSdfContent = new StringBuilder();

            using (var reader = new StringReader(sdfContent))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    // Example: Split the line into columns based on CSV commas
                    var columns = line.Split(',');

                    // Format the CSV content: Join the columns using pipes or other separators for better display
                    formattedSdfContent.AppendLine(string.Join(" | ", columns));  // Customize this logic as needed
                }
            }

            return formattedSdfContent.ToString();
        }

        private bool CanECSSaveFile()
        {
            return true;
        }

        private async void OnSaveECSFileClick()
        {
            IMsgBoxService? msgBoxService = Ioc.Default.GetService<IMsgBoxService>();

            try
            {
                // Define the App.Data folder path
                string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AcesysConversion");

                // Create a map of file names to their paths in App.Data
                var filesToSave = new Dictionary<string, string>
        {
            { "Core.Ref.xml", Path.Combine(appDataPath, "Core.Ref.xml") },
            { "CLX.Config.xml", Path.Combine(appDataPath, "CLX.Config.xml") },
            { "Pnt.Config.xml", Path.Combine(appDataPath, "Pnt.Config.xml") }
        };

                // Prompt the user to select a save location
                var dialog = new FolderBrowserDialog();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedPath = dialog.SelectedPath;

                    foreach (var file in filesToSave)
                    {
                        
                        if (File.Exists(file.Value))
                        {
                            string content = File.ReadAllText(file.Value);                            
                            string newFileName = GetNewFileNameBasedOnSomeLogic(file.Key);
                            string savePath = Path.Combine(selectedPath, newFileName);                           
                            await FileService.SaveXmlFileAsync(savePath, content);                            
                        }
                        else
                        {
                            
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage(msgBoxService, ex);
            }
            finally
            {
                RaiseCanExecuteChanged();
            }
        }

        // Example method to generate a new file name
        private string GetNewFileNameBasedOnSomeLogic(string originalFileName)
        {
            // Implement logic to generate a new file name based on the original file name
            // For example, append a timestamp or use a specific naming convention

            return $"{Path.GetFileNameWithoutExtension(originalFileName)}{Path.GetExtension(originalFileName)}";
        }
        private bool CanECSOpenFile()
        {
            return true;
        }

        private async void OnOpenECSFileClick()
        {
            try
            {
                IOpenFileDlgVM? dialog = Ioc.Default.GetService<IOpenFileDlgVM>();

                if (dialog != null)
                {
                    // Clear previous file paths
                    ECSFilePaths.Clear();
                    Items.Clear(); // Clear previous file names

                    // Loop to select three XML files
                    for (int i = 0; i < 3; i++)
                    {
                        string? sPath = dialog.OpenFileDlg("Select XML File", "XML Files (*.xml)|*.xml");
                        if (!string.IsNullOrEmpty(sPath))
                        {
                            // Add the full path to ECSFilePaths
                            ECSFilePaths.Add(sPath);

                            // Extract the file name and add it to _ecsFileContent
                            string fileName = Path.GetFileName(sPath);
                            Items.Add(fileName);
                        }
                        else
                        {
                            IMsgBoxService? msgBoxService = Ioc.Default.GetService<IMsgBoxService>();
                            return; // Exit the method if the user cancels the dialog
                        }
                    }

                    // Proceed only if exactly three files are selected
                    if (ECSFilePaths.Count == 3)
                    {
                        SetBusyStatus();

                        var fileContents = new Dictionary<string, string>();

                        // Process each selected file
                        foreach (var filePath in ECSFilePaths)
                        {
                            string newFileName = GetNewFileName(filePath);

                            if (string.IsNullOrEmpty(newFileName))
                            {
                                IMsgBoxService? msgBoxService = Ioc.Default.GetService<IMsgBoxService>();
                                return;
                            }

                            string content = await File.ReadAllTextAsync(filePath);
                            await File.WriteAllTextAsync(newFileName, content);

                            // Store the content based on the new file name
                            fileContents[newFileName] = content;

                            // Assign content to appropriate variables
                            if (newFileName == "Core.Ref.xml")
                            {
                                coreRefXml = content;
                            }
                            else if (newFileName == "CLX.Config.xml")
                            {
                                clxConfigXml = content;
                            }
                            else if (newFileName == "Pnt.Config.xml")
                            {
                                pntConfigXml = content;
                            }
                        }

                        SetReadyStatus();

                        // Save the processed XML files to their respective locations
                        foreach (var file in fileContents)
                        {
                            if (!string.IsNullOrEmpty(file.Value))
                            {
                                await FileService.SaveXmlFileAsync(file.Key, file.Value);
                                // Optionally, provide user feedback here
                                // ShowInfoMessage($"File '{file.Key}' saved successfully.");
                            }
                        }
                    }
                    else
                    {
                        IMsgBoxService? msgBoxService = Ioc.Default.GetService<IMsgBoxService>();
                    }
                }
            }
            catch (Exception ex)
            {
                IMsgBoxService? msgBoxService = Ioc.Default.GetService<IMsgBoxService>();
                ShowErrorMessage(msgBoxService, ex);
            }
            finally
            {
                RaiseCanExecuteChanged();
            }
        }

        private string GetNewFileName(string filePath)
        {
            var fileName = Path.GetFileName(filePath);

            if (Regex.IsMatch(fileName, @"Fls\.Core\.Ref\.xml", RegexOptions.IgnoreCase))
            {
                return "Core.Ref.xml";
            }
            if (Regex.IsMatch(fileName, @"Fls\.Ecc\.PLC\.Config\.xml", RegexOptions.IgnoreCase))
            {
                return "CLX.Config.xml";
            }
            if (Regex.IsMatch(fileName, @"Fls\.Ecc\.Pnt\.Config\.xml", RegexOptions.IgnoreCase))
            {
                return "Pnt.Config.xml";
            }

            return string.Empty; // Return empty if no match is found
        }

        private void DispatcherTimer_Tick(object? sender, EventArgs e)
        {
            TimeElapsed = Math.Ceiling(TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds).TotalSeconds).ToString();
        }

        private void ProgressHandler(string message)
        {
            ProgressText = message;
        }

        private void UserMessageSelectionChangedHandler(int reference, int originalReference)
        {
            if (reference <= 0) return;

            TargetCurrentLine = -1;
            TargetCurrentLine = targetIdToLineMapping.FirstOrDefault(l => l.Key == reference).Value;

            SourceCurrentLine = -1;
            SourceCurrentLine = sourceIdToLineMapping.FirstOrDefault(l => l.Key == originalReference).Value;
        }

        private void OnUserMessageDoubleClick(object? sender)
        {
            //var msgBoxService = Ioc.Default.GetService<IMsgBoxService>();

            //if (sender != null)
            //{
            //    UserMessage item = (UserMessage)sender;
            //    try
            //    {
            //        if (xmlDocumentReplaced != null)
            //        {
            //            XmlElement? node = xmlDocumentReplaced.FlatMap[item.Sequence] as XmlElement;
            //            TreeVisualizerChildWindow childWindow = new(node, item.Sequence);
            //            _ = childWindow.ShowDialog();
            //        }
            //    }
            //    catch (KeyNotFoundException)
            //    {
            //        ShowErrorMessage(msgBoxService, new Exception("The item may have been removed during remove operation"));
            //    }
            //    catch (Exception ex)
            //    {
            //        ShowErrorMessage(msgBoxService, ex);
            //    }
            //}
        }

        private bool CanSaveFile()
        {
            return IsDirty;
        }

        private async void OnSaveFileClick()
        {
            IMsgBoxService? msgBoxService = Ioc.Default.GetService<IMsgBoxService>();

            try
            {
                string? sPath;
                ISaveFileDlgVM? dialog = Ioc.Default.GetService<ISaveFileDlgVM>();

                if (dialog != null)
                {
                    sPath = dialog.SaveFileDlg("L5X", "L5X|*.l5x|XML|*.xml");
                    if (!string.IsNullOrEmpty(sPath))
                    {
                        if (xmlDocumentReplaced.HasChildNodes)
                        {
                            targetXmlText = await Task.Run(() => GetFormattedXml(xmlDocumentReplaced));
                            TargetWithoutIdXmlText = await Task.Run(() => RemoveFlsIdAttributes(targetXmlText!, targetIdToLineMapping));
                        }

                        // Save TargetWithoutIdXmlText directly to the specified path
                        if (!string.IsNullOrEmpty(TargetWithoutIdXmlText))
                        {
                            await File.WriteAllTextAsync(sPath, TargetWithoutIdXmlText);
                            ShowInfoMessage(msgBoxService, "File saved successfully");
                            IsDirty = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage(msgBoxService, ex);
            }
            finally
            {
                RaiseCanExecuteChanged();
            }
        }

        private bool CanCloseFile()
        {
            return IsFileLoaded && IsNotUpgrading;
        }

        private void OnCloseFileClick()
        {
            xmlDocument = new RockwellL5XProject();
            xmlDocumentReplaced = new RockwellL5XProject();
            messagesBroker.FileClosed();
            sourceXmlText = string.Empty;
            targetXmlText = string.Empty;
            TargetWithoutIdXmlText = string.Empty;
            SourceWithoutIdXmlText = string.Empty;
            FileName = string.Empty;
            IsFileLoaded = false;
            IsDirty = false;
            RaiseCanExecuteChanged();
            progress.Report(string.Empty);
            TimeElapsed = string.Empty;
            Items.Clear();
        }

        private bool CanUpgradeFile()
        {
            return IsFileLoaded;
        }

        private async void OnUpgradeFileClick()
        {
            try
            {
                BeforeUpgradeSetup();
                xmlDocumentReplaced = await Task.Run(() => UpgradeFile(progress));
                progress.Report("Upgrade Complete, Formatting File");
                if (xmlDocumentReplaced.HasChildNodes)
                {
                    TargetWithoutIdXmlText += "\n\nUpgrading Completed. Loading Upgraded Content, Please Wait...";
                    targetXmlText = await Task.Run(() => GetFormattedXml(xmlDocumentReplaced));
                    TargetWithoutIdXmlText = await Task.Run(() => RemoveFlsIdAttributes(targetXmlText!, targetIdToLineMapping));
                }
                AfterUpgradeSetup();
            }
            catch (Exception ex)
            {
                IMsgBoxService? msgBoxService = Ioc.Default.GetService<IMsgBoxService>();
                ShowErrorMessage(msgBoxService, ex);
            }
            finally
            {
                IsNotUpgrading = true;
                RaiseCanExecuteChanged();
            }
        }

        private void AfterUpgradeSetup()
        {
            IsNotUpgrading = true;
            progress.Report("Upgrade Done");
            IsDirty = true;
            SetReadyStatus();
            stopwatch.Stop();
            stopwatch.Reset();
            dispatcherTimer.Stop();
        }

        private void BeforeUpgradeSetup()
        {
            IsNotUpgrading = false;
            TargetWithoutIdXmlText = "Upgrading, Please Wait...";
            dispatcherTimer.Start();
            stopwatch.Start();
            SetBusyStatus();
            messagesBroker.FileClosed();
            targetIdToLineMapping.Clear();
            RaiseCanExecuteChanged();
        }

        private string RemoveFlsIdAttributes(string xmlText, List<KeyValuePair<int, int>> IdToLineMapping)
        {
            StringBuilder sb = new();

            using StringReader reader = new(xmlText);
            int lineNumber = 1;
            int id = 0;
            Regex regexExist = FlsUiIdentifierExistRegEx();
            Regex regexReplace = FlsUiIdentifierReplaceRegEx();
            Match match;

            for (string? line = reader.ReadLine(); line != null; line = reader.ReadLine())
            {
                match = regexExist.Match(line);
                if (match.Success)
                {
                    id = int.Parse(match.Value);
                    IdToLineMapping.Add(new KeyValuePair<int, int>(id, lineNumber));
                }
                _ = sb.AppendLine($"{regexReplace.Replace(line, "")}");
                
                lineNumber++;
            }

            return sb.ToString();
        }

        private bool CanOpenFile()
        {
            return !IsFileLoaded;
        }

        private async void OnOpenFileClick()
        {
            try
            {
                string? sPath;
                IOpenFileDlgVM? dialog = Ioc.Default.GetService<IOpenFileDlgVM>();

                if (dialog != null)
                {
                    sPath = dialog.OpenFileDlg("L5X", "L5X Files (*.l5x)|*.l5x|XML Files (*.xml)|*.xml|All Files (*.*)|*.*");
                    if (!string.IsNullOrEmpty(sPath))
                    {
                        FileName = sPath;
                        SetBusyStatus();
                        await LoadFile(progress);
                        SetReadyStatus();
                    }
                }
            }
            catch (Exception ex)
            {
                IMsgBoxService? msgBoxService = Ioc.Default.GetService<IMsgBoxService>();
                ShowErrorMessage(msgBoxService, ex);
            }
            finally
            {
                RaiseCanExecuteChanged();
            }
        }

        private void SetReadyStatus()
        {
            LoadingIndicatorText = "Ready...";
            LoadingIndicatorBackground = Brushes.Green;
        }

        private void SetBusyStatus()
        {
            LoadingIndicatorText = "Please Wait...";
            LoadingIndicatorBackground = Brushes.Red;
        }

        private async Task LoadFile(IProgress<string> progress)
        {

            if (FileName != null)
            {
                SourceWithoutIdXmlText = "Please Wait. Loading File...";
                progress.Report("Loading File");
                await Task.Run(() => xmlDocument.Load(FileName));
                if (xmlDocument != null)
                {
                    sourceXmlText = await Task.Run(() => GetFormattedXml(xmlDocument));
                    if (sourceXmlText != null)
                    {
                        SourceWithoutIdXmlText = await Task.Run(() => RemoveFlsIdAttributes(sourceXmlText, sourceIdToLineMapping));
                    }
                }
            }
            progress.Report("File Load Complete");
            IsFileLoaded = true;
        }

        private async Task LoadFileAWL(IProgress<string> progress)
        {
            if (FileName != null)
            {
                SourceWithoutIdAWLText = "Please Wait. Loading File...";
                progress.Report("Loading File");

                string fileExtension = Path.GetExtension(FileName)?.ToLower();
                
                if (fileExtension == ".awl")
                {
                    // Process AWL file as plain text
                    SourceWithoutIdAWLText = await Task.Run(() => File.ReadAllText(FileName));
                    progress.Report("Parsing AWL file...");

                    // Insert custom logic here for processing AWL file content if needed
                    sourceAWLText = await Task.Run(() => FormatAwlContent(SourceWithoutIdAWLText));

                    SourceWithoutIdXmlText = sourceAWLText;
                }

                progress.Report("File Load Complete");
                IsFileLoaded = true;
            }
        }

        private static string FormatAwlContent(string awlContent)
        {            
            return awlContent;
        }

        private static string GetFormattedXml(XmlDocument doc)
        {
            MemoryStream mst = new();
            XmlTextWriter writer = new(mst, Encoding.UTF8)
            {
                Formatting = Formatting.Indented
            };
            doc.WriteContentTo(writer);
            writer.Flush();

            StreamReader sr = new(mst);
            _ = sr.BaseStream.Seek(0, SeekOrigin.Begin);
            string? xmlStr = sr.ReadToEnd();
            mst.Close();
            sr.Close();
            writer.Close();

            return xmlStr;
        }

        private async Task<RockwellL5XProject> UpgradeFile(IProgress<string> progress)
        {
            RockwellL5XProject project = new();
            project.Attach(messagesBroker);

            if (SourceWithoutIdXmlText != null)
            {
                progress.Report("Loading Xml");
                await Task.Run(() => project.LoadXml(SourceWithoutIdXmlText));
            }
            else
            {
                return project;
            }

            L5XController? rockwellController = project?.Content?.Controller;
            if (rockwellController != null)
            {
                RockwellUpgradeOptions options = new()
                {
                    FromVersion = AcesysVersions.V77,
                    ToVersion = AcesysVersions.V80,
                    IsExtendedSelect = BlockSelect.Equals(BlockSelection.Extended),
                    IsExtendedInterlock = InterlockSelect.Equals(InterlockSelection.Extended),
                    IsMapByFunction = MapBy.Equals(FacePlateAttributeMapping.Function),
                    NewFileName = FileName,
                    ECSFilePaths = ECSFilePaths
                    
                };

                await Task.Run(() => rockwellController.UpgradeVersion(xmlDocument, options, progress));
            }

            return project!;
        }

        private async Task<SiemensProject> UpgradeFileS7(IProgress<string> progress)
        {
            SiemensProject project = new();
            project.Attach(messagesBroker);

            // Ensure both AWL and SDF content exist before proceeding
            if (!string.IsNullOrEmpty(SourceWithoutIdXmlText) && !string.IsNullOrEmpty(sourceSDFText))
            {
                progress.Report("Loading AWL and SDF Files");

                // Load AWL and SDF files concurrently
                await Task.WhenAll(
                    Task.Run(() => project.LoadAwlFile(SourceWithoutIdXmlText)),
                    Task.Run(() => project.LoadSdfFile(sourceSDFText))
                );
            }
            else
            {
                // Return early if the necessary content is missing
                return project;
            }

            if (project != null)
            {
                // Create upgrade options
                SiemensUpgradeOptions options = new()
                {
                    FromVersion = AcesysVersions.V77,
                    ToVersion = AcesysVersions.V80,
                    IsExtendedSelect = BlockSelect.Equals(BlockSelection.Extended),
                    IsExtendedInterlock = InterlockSelect.Equals(InterlockSelection.Extended),
                    IsMapByFunction = MapBy.Equals(FacePlateAttributeMapping.Function),
                    NewFileName = FileName,
                    ECSFilePaths = ECSFilePaths
                };

                // Perform the upgrade using both files
                progress.Report("Upgrading project version");
                await Task.Run(() => project.UpgradeVersion(project, options, progress));
            }

            return project!;
        }

        private void RaiseCanExecuteChanged()
        {
            OpenFileCommand?.NotifyCanExecuteChanged();
            CloseFileCommand?.NotifyCanExecuteChanged();
            UpgradeFileCommand?.NotifyCanExecuteChanged();
            SaveFileCommand?.NotifyCanExecuteChanged();
        }

        [GeneratedRegex("(?<=\\bFLS_UI_IDENTIFIER=\")[^\"]*")]
        private static partial Regex FlsUiIdentifierExistRegEx();

        [GeneratedRegex(" FLS_UI_IDENTIFIER=\"(\\d+)\"")]
        private static partial Regex FlsUiIdentifierReplaceRegEx();
    }
}