﻿// ***********************************************************************
// Assembly         : IronyModManager
// Author           : Mario
// Created          : 03-20-2020
//
// Last Modified By : Mario
// Last Modified On : 09-07-2020
// ***********************************************************************
// <copyright file="MergeViewerControlView.xaml.cs" company="Mario">
//     Mario
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using IronyModManager.Common.Views;
using IronyModManager.DI;
using IronyModManager.Shared;
using IronyModManager.ViewModels.Controls;
using ReactiveUI;

namespace IronyModManager.Views.Controls
{
    /// <summary>
    /// Class MergeViewerControlView.
    /// Implements the <see cref="IronyModManager.Common.Views.BaseControl{IronyModManager.ViewModels.Controls.MergeViewerControlViewModel}" />
    /// </summary>
    /// <seealso cref="IronyModManager.Common.Views.BaseControl{IronyModManager.ViewModels.Controls.MergeViewerControlViewModel}" />
    [ExcludeFromCoverage("This should be tested via functional testing.")]
    public class MergeViewerControlView : BaseControl<MergeViewerControlViewModel>
    {
        #region Fields

        /// <summary>
        /// The PDX script
        /// </summary>
        private static IHighlightingDefinition pdxScriptHighlightingDefinition;

        /// <summary>
        /// The yaml highlighting definition
        /// </summary>
        private static IHighlightingDefinition yamlHighlightingDefinition;

        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// The last left data source
        /// </summary>
        private IEnumerable lastLeftDataSource = null;

        /// <summary>
        /// The last right data source
        /// </summary>
        private IEnumerable lastRightDataSource = null;

        /// <summary>
        /// The left side cached menu items
        /// </summary>
        private Dictionary<object, List<MenuItem>> leftSideCachedMenuItems = new Dictionary<object, List<MenuItem>>();

        /// <summary>
        /// The right side cached menu items
        /// </summary>
        private Dictionary<object, List<MenuItem>> rightSideCachedMenuItems = new Dictionary<object, List<MenuItem>>();

        /// <summary>
        /// The syncing scroll
        /// </summary>
        private bool syncingScroll = false;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MergeViewerControlView"/> class.
        /// </summary>
        public MergeViewerControlView()
        {
            logger = DIResolver.Get<ILogger>();
            this.InitializeComponent();
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Focuses the conflict.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="leftListBox">The left ListBox.</param>
        /// <param name="rightListBox">The right ListBox.</param>
        protected virtual void FocusConflict(int line, ListBox leftListBox, ListBox rightListBox)
        {
            leftListBox.SelectedIndex = line;
            rightListBox.SelectedIndex = line;
        }

        /// <summary>
        /// Gets the highlighting definition.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>IHighlightingDefinition.</returns>
        protected virtual IHighlightingDefinition GetHighlightingDefinition(string path)
        {
            var bytes = ResourceReader.GetEmbeddedResource(path);
            var xml = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
            using var sr = new StringReader(xml);
            using var reader = XmlReader.Create(sr);
            return HighlightingLoader.Load(HighlightingLoader.LoadXshd(reader), HighlightingManager.Instance);
        }

        /// <summary>
        /// Handles the context menu.
        /// </summary>
        /// <param name="listBox">The list box.</param>
        /// <param name="leftSide">if set to <c>true</c> [left side].</param>
        protected virtual void HandleContextMenu(ListBox listBox, bool leftSide)
        {
            var lastDataSource = leftSide ? lastLeftDataSource : lastRightDataSource;
            var hoveredItem = listBox.GetLogicalChildren().Cast<ListBoxItem>().FirstOrDefault(p => p.IsPointerOver);
            if (hoveredItem != null)
            {
                var grid = hoveredItem.GetLogicalChildren().OfType<Grid>().FirstOrDefault();
                if (grid != null)
                {
                    var processedItems = leftSide ? leftSideCachedMenuItems : rightSideCachedMenuItems;
                    bool retrieved = processedItems.TryGetValue(hoveredItem.Content, out var cached);
                    if (listBox.Items != lastDataSource || (retrieved && cached != grid.ContextMenu?.Items))
                    {
                        if (leftSide)
                        {
                            leftSideCachedMenuItems = new Dictionary<object, List<MenuItem>>();
                            lastLeftDataSource = listBox.Items;
                        }
                        else
                        {
                            rightSideCachedMenuItems = new Dictionary<object, List<MenuItem>>();
                            lastRightDataSource = listBox.Items;
                        }
                    }
                    if (!processedItems.ContainsKey(hoveredItem.Content))
                    {
                        List<MenuItem> menuItems = null;
                        if (ViewModel.RightSidePatchMod && ViewModel.LeftSidePatchMod)
                        {
                            grid.ContextMenu = null;
                        }
                        else if (!ViewModel.RightSidePatchMod && !ViewModel.LeftSidePatchMod)
                        {
                            menuItems = GetNonEditableMenuItems(leftSide);
                        }
                        else
                        {
                            if (leftSide)
                            {
                                menuItems = ViewModel.RightSidePatchMod ? GetActionsMenuItems(leftSide) : GetEditableMenuItems(leftSide);
                            }
                            else
                            {
                                menuItems = ViewModel.LeftSidePatchMod ? GetActionsMenuItems(leftSide) : GetEditableMenuItems(leftSide);
                            }
                        }
                        if (menuItems != null)
                        {
                            if (grid.ContextMenu == null)
                            {
                                grid.ContextMenu = new ContextMenu();
                            }
                            grid.ContextMenu.Items = menuItems;
                        }
                        processedItems.Add(hoveredItem.Content, menuItems);
                    }
                }
            }
        }

        /// <summary>
        /// Handles the listbox property changed.
        /// </summary>
        /// <param name="args">The <see cref="AvaloniaPropertyChangedEventArgs" /> instance containing the event data.</param>
        /// <param name="thisListBox">The this ListBox.</param>
        /// <param name="otherListBox">The other ListBox.</param>
        protected virtual void HandleListBoxPropertyChanged(AvaloniaPropertyChangedEventArgs args, ListBox thisListBox, ListBox otherListBox)
        {
            if (args.Property == ListBox.ScrollProperty && thisListBox.Scroll != null)
            {
                var scroll = (ScrollViewer)thisListBox.Scroll;
                scroll.PropertyChanged += (scrollSender, scrollArgs) =>
                {
                    if (scrollArgs.Property == ScrollViewer.HorizontalScrollBarValueProperty || scrollArgs.Property == ScrollViewer.VerticalScrollBarValueProperty)
                    {
                        if (thisListBox.Scroll == null ||
                            otherListBox.Scroll == null ||
                            syncingScroll ||
                            (otherListBox.Scroll.Offset.X == thisListBox.Scroll.Offset.X && otherListBox.Scroll.Offset.Y == thisListBox.Scroll.Offset.Y))
                        {
                            return;
                        }
                        syncingScroll = true;
                        Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            await SyncScrollAsync(thisListBox, otherListBox);
                            syncingScroll = false;
                        });
                    }
                };
            }
        }

        /// <summary>
        /// Called when [activated].
        /// </summary>
        /// <param name="disposables">The disposables.</param>
        protected override void OnActivated(CompositeDisposable disposables)
        {
            var editorLeft = this.FindControl<IronyModManager.Controls.TextEditor>("editorLeft");
            var editorRight = this.FindControl<IronyModManager.Controls.TextEditor>("editorRight");
            SetEditorOptions(editorLeft, true);
            SetEditorOptions(editorRight, false);

            var leftSide = this.FindControl<ListBox>("leftSide");
            var rightSide = this.FindControl<ListBox>("rightSide");

            leftSide.PropertyChanged += (sender, args) =>
            {
                HandleListBoxPropertyChanged(args, leftSide, rightSide);
            };
            rightSide.PropertyChanged += (sender, args) =>
            {
                HandleListBoxPropertyChanged(args, rightSide, leftSide);
            };
            leftSide.PointerMoved += (sender, args) =>
            {
                HandleContextMenu(leftSide, true);
            };
            rightSide.PointerMoved += (sender, args) =>
            {
                HandleContextMenu(rightSide, false);
            };
            ViewModel.ConflictFound += (line) =>
            {
                FocusConflict(line, leftSide, rightSide);
            };
            ViewModel.StackChanged += (sender, args) =>
            {
                leftSideCachedMenuItems = new Dictionary<object, List<MenuItem>>();
                rightSideCachedMenuItems = new Dictionary<object, List<MenuItem>>();
            };

            base.OnActivated(disposables);
        }

        /// <summary>
        /// Called when [locale changed].
        /// </summary>
        /// <param name="newLocale">The new locale.</param>
        /// <param name="oldLocale">The old locale.</param>
        protected override void OnLocaleChanged(string newLocale, string oldLocale)
        {
            leftSideCachedMenuItems = new Dictionary<object, List<MenuItem>>();
            rightSideCachedMenuItems = new Dictionary<object, List<MenuItem>>();
            base.OnLocaleChanged(newLocale, oldLocale);
        }

        /// <summary>
        /// Sets the editor options.
        /// </summary>
        /// <param name="editor">The editor.</param>
        /// <param name="leftSide">if set to <c>true</c> [left side].</param>
        protected virtual void SetEditorOptions(TextEditor editor, bool leftSide)
        {
            var ctx = new ContextMenu
            {
                Items = new List<MenuItem>()
                {
                    new MenuItem()
                    {
                        Header = ViewModel.EditorCopy,
                        Command = ReactiveCommand.Create(() =>  editor.Copy()).DisposeWith(Disposables)
                    },
                    new MenuItem()
                    {
                        Header = ViewModel.EditorCut,
                        Command = ReactiveCommand.Create(() =>  editor.Cut()).DisposeWith(Disposables)
                    },
                    new MenuItem()
                    {
                        Header = ViewModel.EditorPaste,
                        Command = ReactiveCommand.Create(() =>  editor.Paste()).DisposeWith(Disposables)
                    },
                    new MenuItem()
                    {
                        Header = ViewModel.EditorDelete,
                        Command = ReactiveCommand.Create(() =>  editor.Delete()).DisposeWith(Disposables)
                    },
                    new MenuItem()
                    {
                        Header = ViewModel.EditorSelectAll,
                        Command = ReactiveCommand.Create(() =>  editor.SelectAll()).DisposeWith(Disposables)
                    },
                    new MenuItem()
                    {
                        Header = "-"
                    },
                    new MenuItem()
                    {
                        Header = ViewModel.EditorUndo,
                        Command = ReactiveCommand.Create(() =>  editor.Undo()).DisposeWith(Disposables)
                    },
                    new MenuItem()
                    {
                        Header = ViewModel.EditorRedo,
                        Command = ReactiveCommand.Create(() =>  editor.Redo()).DisposeWith(Disposables)
                    }
                }
            };
            editor.ContextMenu = ctx;

            editor.Options = new TextEditorOptions()
            {
                ConvertTabsToSpaces = true,
                IndentationSize = 4
            };
            editor.TextArea.ActiveInputHandler = new Implementation.AvaloniaEdit.TextAreaInputHandler(editor);

            ViewModel.WhenAnyValue(p => p.EditingYaml).Subscribe(s =>
            {
                setEditMode();
            }).DisposeWith(Disposables);
            setEditMode();

            void setEditMode()
            {
                if (ViewModel.EditingYaml)
                {
                    if (yamlHighlightingDefinition == null)
                    {
                        yamlHighlightingDefinition = GetHighlightingDefinition(Constants.Resources.YAML);
                    }
                    editor.SyntaxHighlighting = yamlHighlightingDefinition;
                }
                else
                {
                    if (pdxScriptHighlightingDefinition == null)
                    {
                        pdxScriptHighlightingDefinition = GetHighlightingDefinition(Constants.Resources.PDXScript);
                    }
                    editor.SyntaxHighlighting = pdxScriptHighlightingDefinition;
                }
            }

            bool manualAppend = false;
            editor.TextChanged += (sender, args) =>
            {
                // It's a hack I know see: https://github.com/AvaloniaUI/AvaloniaEdit/issues/99.
                // I'd need to go into the code to fix it and it ain't worth it. There doesn't seem to be any feedback on this issue as well.
                var lines = editor.Text.SplitOnNewLine(false).ToList();
                if (lines.Count() > 3)
                {
                    if (manualAppend)
                    {
                        manualAppend = false;
                        return;
                    }
                    var carretOffset = editor.CaretOffset;
                    for (int i = 1; i <= 3; i++)
                    {
                        var last = lines[lines.Count() - i];
                        if (!string.IsNullOrWhiteSpace(last))
                        {
                            manualAppend = true;
                            editor.AppendText("\r\n");
                        }
                    }
                    if (manualAppend)
                    {
                        editor.CaretOffset = carretOffset;
                    }
                }
                lines = editor.Text.SplitOnNewLine().ToList();
                string text = string.Join(Environment.NewLine, lines);
                ViewModel.CurrentEditText = text;
            };
        }

        /// <summary>
        /// Synchronizes the scroll asynchronous.
        /// </summary>
        /// <param name="thisListBox">The this ListBox.</param>
        /// <param name="otherListBox">The other ListBox.</param>
        /// <returns>Task.</returns>
        protected virtual Task SyncScrollAsync(ListBox thisListBox, ListBox otherListBox)
        {
            var thisMaxX = Math.Abs(thisListBox.Scroll.Extent.Width - thisListBox.Scroll.Viewport.Width);
            var thisMaxY = Math.Abs(thisListBox.Scroll.Extent.Height - thisListBox.Scroll.Viewport.Height);
            var otherMaxX = Math.Abs(otherListBox.Scroll.Extent.Width - otherListBox.Scroll.Viewport.Width);
            var otherMaxY = Math.Abs(otherListBox.Scroll.Extent.Height - otherListBox.Scroll.Viewport.Height);
            var offset = thisListBox.Scroll.Offset;
            if (thisListBox.Scroll.Offset.X > otherMaxX || thisListBox.Scroll.Offset.X == thisMaxX)
            {
                offset = offset.WithX(otherMaxX);
            }
            if (thisListBox.Scroll.Offset.Y > otherMaxY || thisListBox.Scroll.Offset.Y == thisMaxY)
            {
                offset = offset.WithY(otherMaxY);
            }
            if (otherListBox.Scroll.Offset.X != offset.X || otherListBox.Scroll.Offset.Y != offset.Y)
            {
                try
                {
                    otherListBox.InvalidateArrange();
                    thisListBox.InvalidateArrange();
                    otherListBox.Scroll.Offset = offset;
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                }
            }
            return Task.FromResult(true);
        }

        /// <summary>
        /// Gets the actions menu items.
        /// </summary>
        /// <param name="leftSide">if set to <c>true</c> [left side].</param>
        /// <returns>List&lt;MenuItem&gt;.</returns>
        private List<MenuItem> GetActionsMenuItems(bool leftSide)
        {
            var menuItems = new List<MenuItem>()
            {
                new MenuItem()
                {
                    Header = ViewModel.NextConflict,
                    Command = ViewModel.NextConflictCommand,
                    CommandParameter = leftSide
                },
                new MenuItem()
                {
                    Header = ViewModel.PrevConflict,
                    Command = ViewModel.PrevConflictCommand,
                    CommandParameter = leftSide
                },
                new MenuItem()
                {
                    Header = "-"
                },
                new MenuItem()
                {
                    Header = ViewModel.CopyText,
                    Command = ViewModel.CopyTextCommand,
                    CommandParameter = leftSide
                },
                new MenuItem()
                {
                    Header = "-" // Separator magic string, and it's documented... NOT really!!!
                },
                new MenuItem()
                {
                    Header = ViewModel.CopyAll,
                    Command = ViewModel.CopyAllCommand,
                    CommandParameter = leftSide
                },
                new MenuItem()
                {
                    Header = ViewModel.CopyThis,
                    Command = ViewModel.CopyThisCommand,
                    CommandParameter = leftSide
                },
                new MenuItem()
                {
                    Header = ViewModel.CopyThisBeforeLine,
                    Command = ViewModel.CopyThisBeforeLineCommand,
                    CommandParameter = leftSide
                },
                new MenuItem()
                {
                    Header = ViewModel.CopyThisAfterLine,
                    Command = ViewModel.CopyThisAfterLineCommand,
                    CommandParameter = leftSide
                }
            };
            return menuItems;
        }

        /// <summary>
        /// Gets the editable menu items.
        /// </summary>
        /// <param name="leftSide">if set to <c>true</c> [left side].</param>
        /// <returns>List&lt;MenuItem&gt;.</returns>
        private List<MenuItem> GetEditableMenuItems(bool leftSide)
        {
            var menuItems = new List<MenuItem>()
            {
                new MenuItem()
                {
                    Header = ViewModel.NextConflict,
                    Command = ViewModel.NextConflictCommand,
                    CommandParameter = leftSide
                },
                new MenuItem()
                {
                    Header = ViewModel.PrevConflict,
                    Command = ViewModel.PrevConflictCommand,
                    CommandParameter = leftSide
                },
                new MenuItem()
                {
                    Header = "-"
                },
                new MenuItem()
                {
                    Header = ViewModel.EditThis,
                    Command = ViewModel.EditThisCommand,
                    CommandParameter = leftSide
                },
                new MenuItem()
                {
                    Header = ViewModel.CopyText,
                    Command = ViewModel.CopyTextCommand,
                    CommandParameter = leftSide
                },
                new MenuItem()
                {
                    Header = "-"
                },
                new MenuItem()
                {
                    Header = ViewModel.DeleteText,
                    Command = ViewModel.DeleteTextCommand,
                    CommandParameter = leftSide
                },
                new MenuItem()
                {
                    Header = ViewModel.MoveUp,
                    Command = ViewModel.MoveUpCommand,
                    CommandParameter = leftSide
                },
                new MenuItem()
                {
                    Header = ViewModel.MoveDown,
                    Command = ViewModel.MoveDownCommand,
                    CommandParameter = leftSide
                },
            };
            var redoAvailable = ViewModel.IsRedoAvailable();
            var undoAvailable = ViewModel.IsUndoAvailable();
            if (redoAvailable || undoAvailable)
            {
                menuItems.Add(new MenuItem()
                {
                    Header = "-"
                });
                if (undoAvailable)
                {
                    menuItems.Add(new MenuItem()
                    {
                        Header = ViewModel.Undo,
                        Command = ViewModel.UndoCommand,
                        CommandParameter = leftSide
                    });
                }
                if (redoAvailable)
                {
                    menuItems.Add(new MenuItem()
                    {
                        Header = ViewModel.Redo,
                        Command = ViewModel.RedoCommand,
                        CommandParameter = leftSide
                    });
                }
            }
            return menuItems;
        }

        /// <summary>
        /// Gets the non editable menu items.
        /// </summary>
        /// <param name="leftSide">The left side.</param>
        /// <returns>System.Collections.Generic.List&lt;Avalonia.Controls.MenuItem&gt;.</returns>
        private List<MenuItem> GetNonEditableMenuItems(bool leftSide)
        {
            var menuItems = new List<MenuItem>()
            {
                new MenuItem()
                {
                    Header = ViewModel.NextConflict,
                    Command = ViewModel.NextConflictCommand,
                    CommandParameter = leftSide
                },
                new MenuItem()
                {
                    Header = ViewModel.PrevConflict,
                    Command = ViewModel.PrevConflictCommand,
                    CommandParameter = leftSide
                },
                new MenuItem()
                {
                    Header = "-"
                },
                new MenuItem()
                {
                    Header = ViewModel.CopyText,
                    Command = ViewModel.CopyTextCommand,
                    CommandParameter = leftSide
                }
            };
            return menuItems;
        }

        /// <summary>
        /// Initializes the component.
        /// </summary>
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        #endregion Methods
    }
}
