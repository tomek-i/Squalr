﻿namespace Ana.Source.Project
{
    using Docking;
    using Main;
    using Microsoft.Win32;
    using Mvvm.Command;
    using ProjectItems;
    using PropertyViewer;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using UserSettings;
    using Utils.Extensions;

    /// <summary>
    /// View model for the Project Explorer
    /// </summary>
    internal class ProjectExplorerViewModel : ToolViewModel
    {
        /// <summary>
        /// The content id for the docking library associated with this view model
        /// </summary>
        public const String ToolContentId = nameof(ProjectExplorerViewModel);

        /// <summary>
        /// The filter to use for saving and loading project filters
        /// </summary>
        public const String ProjectExtensionFilter = "Cheat File (*.Hax)|*.hax|All files (*.*)|*.*";

        /// <summary>
        /// Singleton instance of the <see cref="ProjectExplorerViewModel" /> class
        /// </summary>
        private static Lazy<ProjectExplorerViewModel> projectExplorerViewModelInstance = new Lazy<ProjectExplorerViewModel>(
                () => { return new ProjectExplorerViewModel(); },
                LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// The collection of project items
        /// </summary>
        private ReadOnlyCollection<ProjectItemViewModel> projectItems;

        /// <summary>
        /// The selected project item
        /// </summary>
        private ProjectItemViewModel selectedProjectItem;

        /// <summary>
        /// Whether or not there are unsaved project changes
        /// </summary>
        private Boolean hasUnsavedChanges;

        /// <summary>
        /// The file path to the project file
        /// </summary>
        private String projectFilePath;

        /// <summary>
        /// Prevents a default instance of the <see cref="ProjectExplorerViewModel" /> class from being created
        /// </summary>
        private ProjectExplorerViewModel() : base("Project Explorer")
        {
            this.ContentId = ProjectExplorerViewModel.ToolContentId;

            // Commands to manipulate project items may not be async due to multi-threading issues when modifying collections
            this.OpenProjectCommand = new RelayCommand(() => this.OpenProject(), () => true);
            this.ImportProjectCommand = new RelayCommand(() => this.ImportProject(), () => true);
            this.SaveProjectCommand = new RelayCommand(() => this.SaveProject(), () => true);
            this.SaveAsProjectCommand = new RelayCommand(() => this.SaveAsProject(), () => true);
            this.AddNewFolderItemCommand = new RelayCommand(() => this.AddNewFolderItem(), () => true);
            this.AddNewAddressItemCommand = new RelayCommand(() => this.AddNewAddressItem(), () => true);
            this.AddNewScriptItemCommand = new RelayCommand(() => this.AddNewScriptItem(), () => true);
            this.DeleteSelectionCommand = new RelayCommand(() => this.DeleteSelection(), () => true);
            this.ToggleSelectionActivationCommand = new RelayCommand(() => this.ToggleSelectionActivation(), () => true);
            this.IsVisible = true;
            this.projectItems = new ReadOnlyCollection<ProjectItemViewModel>(new List<ProjectItemViewModel>());
            this.Update();

            Task.Run(() => MainViewModel.GetInstance().Subscribe(this));
        }

        /// <summary>
        /// Gets the command to open a project from disk
        /// </summary>
        public ICommand OpenProjectCommand { get; private set; }

        /// <summary>
        /// Gets the command to open a project from disk
        /// </summary>
        public ICommand ImportProjectCommand { get; private set; }

        /// <summary>
        /// Gets the command to open a project from disk
        /// </summary>
        public ICommand SaveProjectCommand { get; private set; }

        /// <summary>
        /// Gets the command to open a project from disk
        /// </summary>
        public ICommand SaveAsProjectCommand { get; private set; }

        /// <summary>
        /// Gets the command to add a new folder
        /// </summary>
        public ICommand AddNewFolderItemCommand { get; private set; }

        /// <summary>
        /// Gets the command to add a new address
        /// </summary>
        public ICommand AddNewAddressItemCommand { get; private set; }

        /// <summary>
        /// Gets the command to add a new script
        /// </summary>
        public ICommand AddNewScriptItemCommand { get; private set; }

        /// <summary>
        /// Gets the command to delete the selected project explorer items
        /// </summary>
        public ICommand DeleteSelectionCommand { get; private set; }

        /// <summary>
        /// Gets the command to toggle the activation the selected project explorer items
        /// </summary>
        public ICommand ToggleSelectionActivationCommand { get; private set; }

        /// <summary>
        /// Gets the command to clear the selected project item
        /// </summary>
        public ICommand ClearSelectionCommand { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether there are unsaved changes
        /// </summary>
        public Boolean HasUnsavedChanges
        {
            get
            {
                return this.hasUnsavedChanges;
            }

            set
            {
                this.hasUnsavedChanges = value;
                this.RaisePropertyChanged(nameof(this.HasUnsavedChanges));
            }
        }

        /// <summary>
        /// Gets the path to the project file
        /// </summary>
        public String ProjectFilePath
        {
            get
            {
                return this.projectFilePath;
            }

            private set
            {
                this.projectFilePath = value;
                this.RaisePropertyChanged(nameof(this.ProjectFilePath));
            }
        }

        /// <summary>
        /// Gets or sets the collection of project items
        /// </summary>
        public ReadOnlyCollection<ProjectItemViewModel> ProjectItems
        {
            get
            {
                return this.projectItems;
            }

            set
            {
                this.projectItems = value;
                this.HasUnsavedChanges = true;
                this.RaisePropertyChanged(nameof(this.ProjectItems));
            }
        }

        /// <summary>
        /// Gets or sets the selected project item
        /// </summary>
        public ProjectItemViewModel SelectedProjectItem
        {
            get
            {
                return this.selectedProjectItem;
            }

            set
            {
                this.selectedProjectItem = value;
                PropertyViewerViewModel.GetInstance().SetTargetObjects(this.selectedProjectItem?.ProjectItem);
                this.RaisePropertyChanged(nameof(this.SelectedProjectItem));
            }
        }

        /// <summary>
        /// Gets a singleton instance of the <see cref="ProjectExplorerViewModel" /> class
        /// </summary>
        /// <returns>A singleton instance of the class</returns>
        public static ProjectExplorerViewModel GetInstance()
        {
            return ProjectExplorerViewModel.projectExplorerViewModelInstance.Value;
        }

        /// <summary>
        /// Removes a project item from the project explorer
        /// </summary>
        /// <param name="projectItemViewModel">The project item view model to be removed</param>
        public void RemoveProjectItem(ProjectItemViewModel projectItemViewModel)
        {
            this.ProjectItems = new ReadOnlyCollection<ProjectItemViewModel>(this.ProjectItems.Where(x => x != projectItemViewModel).ToList());
            this.SelectedProjectItem = null;
        }

        /// <summary>
        /// Inserts a project item into the project explorer
        /// </summary>
        /// <param name="projectItemViewModel">The project item view model to be removed</param>
        /// <param name="index">The index of insertion</param>
        public void InsertProjectItem(ProjectItemViewModel projectItemViewModel, Int32 index)
        {
            List<ProjectItemViewModel> newItems = this.ProjectItems.ToList();
            newItems.Insert(index, projectItemViewModel);
            this.ProjectItems = new ReadOnlyCollection<ProjectItemViewModel>(newItems);
        }

        /// <summary>
        /// Adds a specific address to the project explorer
        /// </summary>
        /// <param name="baseAddress">The address</param>
        /// <param name="elementType">The value type</param>
        public void AddSpecificAddressItem(IntPtr baseAddress, Type elementType)
        {
            this.AddNewProjectItem(new AddressItem(baseAddress, elementType));
        }

        /// <summary>
        /// Adds the new project item to the project item collection
        /// </summary>
        /// <param name="projectItem">The project item to add</param>
        public void AddNewProjectItem(ProjectItem projectItem)
        {
            List<ProjectItemViewModel> newItems = new List<ProjectItemViewModel>(this.ProjectItems);

            ProjectItemViewModel folderTarget = this.SelectedProjectItem;

            while (folderTarget != null && !(folderTarget.ProjectItem is FolderItem))
            {
                folderTarget = folderTarget.Parent as ProjectItemViewModel;
            }

            if (folderTarget != null)
            {
                folderTarget.AddChild(new ProjectItemViewModel(projectItem));
            }
            else
            {
                newItems.Add(new ProjectItemViewModel(projectItem));
            }

            this.ProjectItems = new ReadOnlyCollection<ProjectItemViewModel>(newItems);
        }

        /// <summary>
        /// Adds a new folder to the project items
        /// </summary>
        private void AddNewFolderItem()
        {
            this.AddNewProjectItem(new FolderItem());
        }

        /// <summary>
        /// Adds a new address to the project items
        /// </summary>
        private void AddNewAddressItem()
        {
            this.AddNewProjectItem(new AddressItem());
        }

        /// <summary>
        /// Adds a new script to the project items
        /// </summary>
        private void AddNewScriptItem()
        {
            this.AddNewProjectItem(new ScriptItem());
        }

        /// <summary>
        /// Deletes the selected project explorer items
        /// </summary>
        private void DeleteSelection()
        {
            this.RemoveProjectItem(this.SelectedProjectItem);
        }

        /// <summary>
        /// Toggles the activation the selected project explorer items
        /// </summary>
        private void ToggleSelectionActivation()
        {
            ProjectItem projectItem = this.SelectedProjectItem?.ProjectItem;
            if (projectItem != null)
            {
                projectItem.IsActivated = !projectItem.IsActivated;
            }
        }

        /// <summary>
        /// Continously updates the project explorer items
        /// </summary>
        private void Update()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    this.ProjectItems.ForEach(x => this.UpdateRecurse(x));
                    Thread.Sleep(SettingsViewModel.GetInstance().TableReadInterval);
                }
            });
        }

        /// <summary>
        /// Recursive helper function to update a project item and all children contained by it
        /// </summary>
        /// <param name="projectItemViewModel">The project item view model being updated</param>
        private void UpdateRecurse(ProjectItemViewModel projectItemViewModel)
        {
            projectItemViewModel?.Children?.ForEach(x => this.UpdateRecurse(x as ProjectItemViewModel));
            projectItemViewModel?.ProjectItem?.Update();
        }

        /// <summary>
        /// Opens a project from disk
        /// </summary>
        private void OpenProject()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = ProjectExtensionFilter;
            openFileDialog.Title = "Open Project";
            openFileDialog.ShowDialog();
            this.ProjectFilePath = openFileDialog.FileName;

            if (this.ProjectFilePath == null || this.ProjectFilePath == String.Empty)
            {
                return;
            }

            try
            {
                using (FileStream fileStream = new FileStream(this.ProjectFilePath, FileMode.Open, FileAccess.Read))
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(ProjectItem[]));
                    ProjectItem[] projectRoots = serializer.ReadObject(fileStream) as ProjectItem[];
                    this.ProjectItems = new ReadOnlyCollection<ProjectItemViewModel>(projectRoots.Select(x => new ProjectItemViewModel(x)).ToList());
                    this.HasUnsavedChanges = false;
                }
            }
            catch
            {
                return;
            }
        }

        /// <summary>
        /// Imports a project from disk, adding the project items to the current project
        /// </summary>
        private void ImportProject()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = ProjectExtensionFilter;
            openFileDialog.Title = "Import Project";
            openFileDialog.ShowDialog();
            this.ProjectFilePath = openFileDialog.FileName;

            if (this.ProjectFilePath == null || this.ProjectFilePath == String.Empty)
            {
                return;
            }

            try
            {
                using (FileStream fileStream = new FileStream(this.ProjectFilePath, FileMode.Open, FileAccess.Read))
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(ProjectItem[]));
                    ProjectItem[] projectRoots = serializer.ReadObject(fileStream) as ProjectItem[];
                    List<ProjectItemViewModel> newItems = projectRoots.Select(x => new ProjectItemViewModel(x)).ToList();
                    newItems.AddRange(this.ProjectItems);
                    this.ProjectItems = new ReadOnlyCollection<ProjectItemViewModel>(newItems);
                    this.HasUnsavedChanges = true;
                }
            }
            catch
            {
                return;
            }
        }

        /// <summary>
        /// Save a project to disk
        /// </summary>
        private void SaveProject()
        {
            if (this.ProjectFilePath == null || this.ProjectFilePath == String.Empty)
            {
                this.SaveAsProject();
                return;
            }

            try
            {
                using (FileStream fileStream = new FileStream(this.ProjectFilePath, FileMode.Create, FileAccess.Write))
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(ProjectItem[]));
                    serializer.WriteObject(fileStream, this.ProjectItems?.Select(x => x.ProjectItem).ToArray());
                }

                this.HasUnsavedChanges = false;
            }
            catch
            {
                return;
            }
        }

        /// <summary>
        /// Save a project to disk under a new specified project name
        /// </summary>
        private void SaveAsProject()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = ProjectExplorerViewModel.ProjectExtensionFilter;
            saveFileDialog.Title = "Save Project";
            saveFileDialog.ShowDialog();
            this.ProjectFilePath = saveFileDialog.FileName;
            this.SaveProject();
        }
    }
    //// End class
}
//// End namespace