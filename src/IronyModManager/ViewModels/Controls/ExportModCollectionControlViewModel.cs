﻿// ***********************************************************************
// Assembly         : IronyModManager
// Author           : Mario
// Created          : 03-09-2020
//
// Last Modified By : Mario
// Last Modified On : 03-09-2020
// ***********************************************************************
// <copyright file="ExportModCollectionControlViewModel.cs" company="Mario">
//     Mario
// </copyright>
// <summary></summary>
// ***********************************************************************
using System.Collections.Generic;
using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using IronyModManager.Common.ViewModels;
using IronyModManager.Implementation;
using IronyModManager.Implementation.Actions;
using IronyModManager.Localization.Attributes;
using IronyModManager.Shared;
using ReactiveUI;

namespace IronyModManager.ViewModels.Controls
{
    /// <summary>
    /// Class ExportModCollectionControlViewModel.
    /// Implements the <see cref="IronyModManager.Common.ViewModels.BaseViewModel" />
    /// </summary>
    /// <seealso cref="IronyModManager.Common.ViewModels.BaseViewModel" />
    public class ExportModCollectionControlViewModel : BaseViewModel
    {
        #region Fields

        /// <summary>
        /// The file dialog action
        /// </summary>
        private readonly IFileDialogAction fileDialogAction;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportModCollectionControlViewModel" /> class.
        /// </summary>
        /// <param name="fileDialogAction">The file dialog action.</param>
        public ExportModCollectionControlViewModel(IFileDialogAction fileDialogAction)
        {
            this.fileDialogAction = fileDialogAction;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether this instance can export.
        /// </summary>
        /// <value><c>true</c> if this instance can export; otherwise, <c>false</c>.</value>
        public virtual bool CanExport { get; set; }

        /// <summary>
        /// Gets or sets the name of the collection.
        /// </summary>
        /// <value>The name of the collection.</value>
        public virtual string CollectionName { get; set; }

        /// <summary>
        /// Gets or sets the export.
        /// </summary>
        /// <value>The export.</value>
        [StaticLocalization(LocalizationResources.Collection_Mods.Export)]
        public virtual string Export { get; protected set; }

        /// <summary>
        /// Gets or sets the export command.
        /// </summary>
        /// <value>The export command.</value>
        public virtual ReactiveCommand<Unit, CommandResult<string>> ExportCommand { get; protected set; }

        /// <summary>
        /// Gets or sets the export dialog title.
        /// </summary>
        /// <value>The export dialog title.</value>
        [StaticLocalization(LocalizationResources.Collection_Mods.Export_Dialog_Title)]
        public virtual string ExportDialogTitle { get; protected set; }

        /// <summary>
        /// Gets or sets the import.
        /// </summary>
        /// <value>The import.</value>
        [StaticLocalization(LocalizationResources.Collection_Mods.Import)]
        public virtual string Import { get; protected set; }

        /// <summary>
        /// Gets or sets the import command.
        /// </summary>
        /// <value>The import command.</value>
        public virtual ReactiveCommand<Unit, CommandResult<string>> ImportCommand { get; protected set; }

        /// <summary>
        /// Gets or sets the import dialog title.
        /// </summary>
        /// <value>The import dialog title.</value>
        [StaticLocalization(LocalizationResources.Collection_Mods.Import_Dialog_Title)]
        public virtual string ImportDialogTitle { get; protected set; }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Called when [activated].
        /// </summary>
        /// <param name="disposables">The disposables.</param>
        protected override void OnActivated(CompositeDisposable disposables)
        {
            ExportCommand = ReactiveCommand.Create(() =>
            {
                if (!CanExport)
                {
                    return new CommandResult<string>(string.Empty, CommandState.NotExecuted);
                }
                var task = Task.Run(async () =>
                {
                    var result = await fileDialogAction.SaveDialogAsync(ExportDialogTitle, CollectionName, Shared.Constants.ZipExtensionWithoutDot);
                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        return result;
                    }
                    return string.Empty;
                });
                Task.WaitAll(task);
                var result = task.Result;
                return new CommandResult<string>(result, !string.IsNullOrWhiteSpace(result) ? CommandState.Success : CommandState.Failed);
            }).DisposeWith(disposables);

            ImportCommand = ReactiveCommand.Create(() =>
            {
                if (!CanExport)
                {
                    return new CommandResult<string>(string.Empty, CommandState.NotExecuted);
                }
                var task = Task.Run(async () =>
                {
                    var result = await fileDialogAction.OpenDialogAsync(ImportDialogTitle, string.Empty, Shared.Constants.ZipExtensionWithoutDot);
                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        return result;
                    }
                    return string.Empty;
                });
                Task.WaitAll(task);
                var result = task.Result;
                return new CommandResult<string>(result, !string.IsNullOrWhiteSpace(result) ? CommandState.Success : CommandState.Failed);
            }).DisposeWith(disposables);

            base.OnActivated(disposables);
        }

        #endregion Methods
    }
}