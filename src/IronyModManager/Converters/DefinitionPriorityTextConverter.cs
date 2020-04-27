﻿// ***********************************************************************
// Assembly         : IronyModManager
// Author           : Mario
// Created          : 04-27-2020
//
// Last Modified By : Mario
// Last Modified On : 04-27-2020
// ***********************************************************************
// <copyright file="DefinitionPriorityTextConverter.cs" company="Mario">
//     Mario
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using IronyModManager.DI;
using IronyModManager.Localization;
using IronyModManager.Models.Common;
using IronyModManager.Parser.Common.Definitions;
using IronyModManager.Services.Common;
using IronyModManager.Shared;

namespace IronyModManager.Converters
{
    /// <summary>
    /// Class DefinitionModTextConverter.
    /// Implements the <see cref="Avalonia.Data.Converters.IMultiValueConverter" />
    /// </summary>
    /// <seealso cref="Avalonia.Data.Converters.IMultiValueConverter" />
    public class DefinitionPriorityTextConverter : IMultiValueConverter
    {
        #region Methods

        /// <summary>
        /// Converts the specified values.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <param name="targetType">Type of the target.</param>
        /// <param name="parameter">The parameter.</param>
        /// <param name="culture">The culture.</param>
        /// <returns>System.Object.</returns>
        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values != null && values.Count == 2)
            {
                if (values[0] is IEnumerable<IDefinition> col && values[1] is IDefinition definition)
                {
                    var service = DIResolver.Get<IModService>();
                    if (!service.IsPatchMod(definition.ModName))
                    {
                        var locManager = DIResolver.Get<ILocalizationManager>();
                        var clean = new List<IDefinition>();
                        bool noPatchMod = true;
                        foreach (var item in col)
                        {
                            if (!service.IsPatchMod(item.ModName))
                            {
                                clean.Add(item);
                            }
                            else
                            {
                                noPatchMod = false;
                            }
                        }
                        if (!noPatchMod)
                        {
                            var priority = service.EvalDefinitionPriority(clean);
                            if (priority?.Definition == definition && priority.PriorityType != DefinitionPriorityType.None)
                            {
                                return priority.PriorityType switch
                                {
                                    DefinitionPriorityType.FIOS => $" {locManager.GetResource(LocalizationResources.Conflict_Solver.PriorityReason.FIOS)}",
                                    DefinitionPriorityType.LIOS => $" {locManager.GetResource(LocalizationResources.Conflict_Solver.PriorityReason.LIOS)}",
                                    DefinitionPriorityType.ModOrder => $" {locManager.GetResource(LocalizationResources.Conflict_Solver.PriorityReason.Order)}",
                                    _ => string.Empty
                                };
                            }
                        }
                    }
                }
            }
            return string.Empty;
        }

        #endregion Methods
    }
}