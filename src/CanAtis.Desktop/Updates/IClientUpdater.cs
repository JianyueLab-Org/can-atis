// <copyright file="IClientUpdater.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Threading.Tasks;

namespace CanAtis.Updates;

/// <summary>
/// Checks for a newer client release.
/// </summary>
public interface IClientUpdater
{
    /// <summary>
    /// Checks release metadata without installing an update.
    /// </summary>
    /// <returns>The available update, or null when current or unavailable.</returns>
    public Task<ClientUpdate?> Run();
}
